using System.Numerics;
using System.Runtime.InteropServices;
using ImGuiNET;

namespace Engine;

/// <summary>
/// Render graph node that draws ImGui draw data using Vulkan.
/// Reads draw data directly from ImGui (valid after ImGui.Render(), before next NewFrame()).
/// Manages its own pipeline and font atlas texture; vertex/index buffers are
/// transiently allocated from the <see cref="DynamicBufferAllocator"/> each frame.
/// </summary>
internal sealed class ImGuiRenderNode : IRenderNode, IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.ImGui.Vulkan");

    public string Name => "imgui";
    public IReadOnlyCollection<string> Dependencies { get; } = new[] { "sample" };

    // Pipeline and font resources (created lazily on first Execute)
    private IPipeline? _pipeline;
    private IShader? _vertexShader;
    private IShader? _fragmentShader;
    private IImage? _fontImage;
    private IImageView? _fontImageView;
    private ISampler? _fontSampler;
    private IDescriptorSet? _fontDescriptorSet;

    public unsafe void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        var drawData = ImGui.GetDrawData();
        if (!drawData.Valid || drawData.CmdListsCount == 0)
            return;

        var gfx = ctx.Graphics;
        var cmd = cmds.FrameContext.CommandBuffer;
        var extent = cmds.FrameContext.Extent;
        var allocator = cmds.DynamicAllocator;

        // Lazy-init pipeline and font atlas
        if (_pipeline is null)
        {
            CreatePipelineAndFontAtlas(gfx, cmds.FrameContext.RenderPass);
        }

        // Calculate total vertex/index sizes
        int totalVertices = drawData.TotalVtxCount;
        int totalIndices = drawData.TotalIdxCount;
        if (totalVertices == 0 || totalIndices == 0)
            return;

        ulong vertexSize = (ulong)(totalVertices * sizeof(ImDrawVert));
        ulong indexSize = (ulong)(totalIndices * sizeof(ushort));

        // Allocate transient vertex/index buffers from the dynamic allocator.
        // The allocator guarantees the backing buffer is idle (fence-waited) so
        // CPU writes here are safe and the allocation lives until next BeginFrame
        // recycles this in-flight slot.
        DynamicAllocation vertexAlloc, indexAlloc;
        if (allocator is not null)
        {
            vertexAlloc = allocator.Allocate(vertexSize, BufferUsage.Vertex);
            indexAlloc = allocator.Allocate(indexSize, BufferUsage.Index);
        }
        else
        {
            return; // No allocator - cannot upload ImGui geometry
        }

        // Upload vertex and index data
        {
            var vtxSpan = allocator.Map(vertexAlloc);
            var idxSpan = allocator.Map(indexAlloc);

            int vtxOffset = 0;
            int idxOffset = 0;
            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                var cmdList = drawData.CmdLists[n];
                int vtxBytes = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
                int idxBytes = cmdList.IdxBuffer.Size * sizeof(ushort);

                new Span<byte>((void*)cmdList.VtxBuffer.Data, vtxBytes)
                    .CopyTo(vtxSpan.Slice(vtxOffset, vtxBytes));
                new Span<byte>((void*)cmdList.IdxBuffer.Data, idxBytes)
                    .CopyTo(idxSpan.Slice(idxOffset, idxBytes));

                vtxOffset += vtxBytes;
                idxOffset += idxBytes;
            }

            allocator.Unmap(vertexAlloc);
            allocator.Unmap(indexAlloc);
        }

        // Build orthographic projection matrix
        float L = drawData.DisplayPos.X;
        float R = drawData.DisplayPos.X + drawData.DisplaySize.X;
        float T = drawData.DisplayPos.Y;
        float B = drawData.DisplayPos.Y + drawData.DisplaySize.Y;

        var projection = new Matrix4x4(
            2.0f / (R - L), 0, 0, 0,
            0, 2.0f / (B - T), 0, 0,
            0, 0, -1.0f, 0,
            -(R + L) / (R - L), -(T + B) / (B - T), 0, 1.0f
        );

        // Set viewport (in framebuffer pixels, accounting for DPI / framebuffer scale)
        float fbScaleX = drawData.FramebufferScale.X;
        float fbScaleY = drawData.FramebufferScale.Y;
        float fbWidth = drawData.DisplaySize.X * fbScaleX;
        float fbHeight = drawData.DisplaySize.Y * fbScaleY;
        if (fbWidth <= 0 || fbHeight <= 0)
            return;

        gfx.SetViewport(cmd, 0, 0, fbWidth, fbHeight, 0, 1);

        // Bind pipeline and resources
        gfx.BindGraphicsPipeline(cmd, _pipeline!);
        gfx.BindDescriptorSet(cmd, _pipeline!, _fontDescriptorSet!);

        // Push projection matrix
        var projBytes = MemoryMarshal.AsBytes(new ReadOnlySpan<Matrix4x4>(in projection));
        gfx.PushConstants(cmd, _pipeline!, ShaderStageFlags.Vertex, 0, projBytes);

        // Bind vertex and index buffers from dynamic allocator
        gfx.BindVertexBuffers(cmd, 0, new[] { vertexAlloc.Buffer }, new ulong[] { vertexAlloc.Offset });
        gfx.BindIndexBuffer(cmd, indexAlloc.Buffer, indexAlloc.Offset, IndexType.UInt16);

        // Render draw commands
        var clipOff = drawData.DisplayPos;
        var clipScale = drawData.FramebufferScale;

        int globalVtxOffset = 0;
        int globalIdxOffset = 0;

        for (int n = 0; n < drawData.CmdListsCount; n++)
        {
            var cmdList = drawData.CmdLists[n];
            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                var pcmd = cmdList.CmdBuffer[i];

                // User callback (not supported, skip)
                if (pcmd.UserCallback != IntPtr.Zero)
                    continue;

                // Apply clip rect
                var clipMin = new Vector2(
                    (pcmd.ClipRect.X - clipOff.X) * clipScale.X,
                    (pcmd.ClipRect.Y - clipOff.Y) * clipScale.Y);
                var clipMax = new Vector2(
                    (pcmd.ClipRect.Z - clipOff.X) * clipScale.X,
                    (pcmd.ClipRect.W - clipOff.Y) * clipScale.Y);

                if (clipMax.X <= clipMin.X || clipMax.Y <= clipMin.Y)
                    continue;

                // Clamp to framebuffer bounds
                int sx = Math.Max(0, (int)clipMin.X);
                int sy = Math.Max(0, (int)clipMin.Y);
                uint sw = (uint)(clipMax.X - sx);
                uint sh = (uint)(clipMax.Y - sy);

                if (sw == 0 || sh == 0) continue;

                gfx.SetScissor(cmd, sx, sy, sw, sh);

                gfx.DrawIndexed(cmd,
                    pcmd.ElemCount,
                    instanceCount: 1,
                    firstIndex: (uint)(pcmd.IdxOffset + globalIdxOffset),
                    vertexOffset: (int)(pcmd.VtxOffset + globalVtxOffset),
                    firstInstance: 0);
            }

            globalVtxOffset += cmdList.VtxBuffer.Size;
            globalIdxOffset += cmdList.IdxBuffer.Size;
        }

        // Reset scissor to full framebuffer
        gfx.SetScissor(cmd, 0, 0, extent.Width, extent.Height);
    }

    private unsafe void CreatePipelineAndFontAtlas(IGraphicsDevice gfx, IRenderPass renderPass)
    {
        Logger.Info("Creating ImGui Vulkan pipeline and font atlas...");

        // Load / compile shaders
        ImGuiShaders.EnsureLoaded();
        var vsDesc = new ShaderDesc(ShaderStage.Vertex, ImGuiShaders.Vertex, "main");
        var fsDesc = new ShaderDesc(ShaderStage.Fragment, ImGuiShaders.Fragment, "main");
        _vertexShader = gfx.CreateShader(vsDesc);
        _fragmentShader = gfx.CreateShader(fsDesc);

        // ImDrawVert layout: pos (vec2, 8 bytes), uv (vec2, 8 bytes), col (uint32, 4 bytes) = 20 bytes
        var vertexBindings = new[]
        {
            new VertexInputBindingDesc(0, (uint)sizeof(ImDrawVert))
        };
        var vertexAttributes = new[]
        {
            new VertexInputAttributeDesc(0, 0, VertexFormat.Float2, 0),         // aPos
            new VertexInputAttributeDesc(1, 0, VertexFormat.Float2, 8),         // aUV
            new VertexInputAttributeDesc(2, 0, VertexFormat.UNormR8G8B8A8, 16) // aColor
        };
        var pushConstants = new[]
        {
            new PushConstantRange(ShaderStageFlags.Vertex, 0, (uint)sizeof(Matrix4x4))
        };

        var pipelineDesc = new GraphicsPipelineDesc(
            renderPass, _vertexShader, _fragmentShader,
            BlendEnabled: true,
            CullBackFace: false,
            VertexBindings: vertexBindings,
            VertexAttributes: vertexAttributes,
            PushConstantRanges: pushConstants);

        _pipeline = gfx.CreateGraphicsPipeline(pipelineDesc);
        Logger.Info("ImGui pipeline created.");

        // Upload font atlas
        var io = ImGui.GetIO();
        io.Fonts.GetTexDataAsRGBA32(out IntPtr pixels, out int width, out int height, out int bytesPerPixel);

        var imageDesc = new ImageDesc(
            new Extent2D((uint)width, (uint)height),
            ImageFormat.R8G8B8A8_UNorm,
            ImageUsage.Sampled | ImageUsage.TransferDst);
        _fontImage = gfx.CreateImage(imageDesc);
        _fontImageView = gfx.CreateImageView(_fontImage);
        _fontSampler = gfx.CreateSampler(new SamplerDesc(
            SamplerFilter.Linear, SamplerFilter.Linear,
            SamplerAddressMode.ClampToEdge, SamplerAddressMode.ClampToEdge,
            SamplerAddressMode.ClampToEdge));

        // Upload pixel data
        int dataSize = width * height * bytesPerPixel;
        var pixelData = new ReadOnlySpan<byte>((void*)pixels, dataSize);
        gfx.UploadTexture2D(_fontImage, pixelData, (uint)width, (uint)height, bytesPerPixel);

        // Create and update descriptor set
        _fontDescriptorSet = gfx.CreateDescriptorSet();
        var samplerBinding = new CombinedImageSamplerBinding(_fontImageView, _fontSampler, 1);
        gfx.UpdateDescriptorSet(_fontDescriptorSet, uniformBinding: null, samplerBinding);

        // Set texture ID and free CPU-side pixels
        io.Fonts.SetTexID((IntPtr)1);
        io.Fonts.ClearTexData();

        Logger.Info($"ImGui font atlas uploaded: {width}x{height} R8G8B8A8_UNorm.");
    }

    public void Dispose()
    {
        _fontDescriptorSet?.Dispose();
        _fontSampler?.Dispose();
        _fontImageView?.Dispose();
        _fontImage?.Dispose();
        _fragmentShader?.Dispose();
        _vertexShader?.Dispose();
    }
}
