using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using UltralightNet;
using UltralightNet.Enums;
using UltralightNet.Platform;
using UltralightNet.Structs;

namespace Engine;

/// <summary>
/// Vulkan-backed implementation of the Ultralight <see cref="IGpuDriverSynchronized"/> interface.
/// Translates Ultralight GPU resource management and draw commands into calls on the engine's
/// <see cref="IGraphicsDevice"/> abstraction, allowing Ultralight to render directly to GPU
/// textures without CPU pixel readback.
/// </summary>
/// <remarks>
/// <para>
/// Ultralight calls the driver methods during <c>Renderer.Render()</c> in two phases:
/// <list type="number">
///   <item><description><b>Synchronization</b>: <see cref="BeginSynchronize"/> → resource Create/Update/Destroy → <see cref="EndSynchronize"/></description></item>
///   <item><description><b>Commands</b>: <see cref="UpdateCommandList"/> receives draw commands which are stored and executed later via <see cref="FlushCommands"/>.</description></item>
/// </list>
/// </para>
/// <para>Thread safety: all calls must occur on the main thread.</para>
/// </remarks>
/// <seealso cref="WebViewInstance"/>
/// <seealso cref="GpuWebViewRenderNode"/>
public sealed class VulkanUltralightGpuDriver : IGpuDriverSynchronized, IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.GpuDriver");

    private readonly IGraphicsDevice _gfx;

    // ── ID generators ────────────────────────────────────────────────
    private uint _nextTextureId = 1;
    private uint _nextRenderBufferId = 1;
    private uint _nextGeometryId = 1;

    // ── Resource maps ────────────────────────────────────────────────
    private readonly Dictionary<uint, TextureEntry> _textures = new();
    private readonly Dictionary<uint, RenderBufferEntry> _renderBuffers = new();
    private readonly Dictionary<uint, GeometryEntry> _geometries = new();

    // ── Deferred command list ────────────────────────────────────────
    private readonly List<StoredCommand> _pendingCommands = new();

    // ── Pipelines (created lazily) ───────────────────────────────────
    private IPipeline? _fillPipeline;
    private IPipeline? _fillPathPipeline;
    private IShader? _fillVs;
    private IShader? _fillFs;
    private IShader? _fillPathVs;
    private IShader? _fillPathFs;

    /// <summary>Whether at least one command list has been received since the last flush.</summary>
    public bool HasPendingCommands => _pendingCommands.Count > 0;

    /// <summary>
    /// Retrieves the <see cref="IImageView"/> for a given Ultralight texture ID.
    /// Used by the render node to composite the final view texture onto the screen.
    /// </summary>
    /// <param name="textureId">Ultralight-assigned texture ID.</param>
    /// <returns>The image view, or <c>null</c> if not found.</returns>
    public IImageView? GetTextureView(uint textureId)
    {
        return _textures.TryGetValue(textureId, out var entry) ? entry.ImageView : null;
    }

    /// <summary>
    /// Retrieves the <see cref="IImage"/> for a given Ultralight texture ID.
    /// </summary>
    public IImage? GetTexture(uint textureId)
    {
        return _textures.TryGetValue(textureId, out var entry) ? entry.Image : null;
    }

    public VulkanUltralightGpuDriver(IGraphicsDevice gfx)
    {
        _gfx = gfx ?? throw new ArgumentNullException(nameof(gfx));
    }

    // ══════════════════════════════════════════════════════════════════
    //  IGpuDriverSynchronized
    // ══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public void BeginSynchronize()
    {
        // Nothing to prepare — all state mutations happen inline.
    }

    /// <inheritdoc />
    public void EndSynchronize()
    {
        // Sync complete.
    }

    // ══════════════════════════════════════════════════════════════════
    //  Textures
    // ══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public uint NextTextureId() => _nextTextureId++;

    /// <inheritdoc />
    public unsafe void CreateTexture(uint textureId, UlBitmap bitmap)
    {
        var width = bitmap.Width;
        var height = bitmap.Height;
        var isEmpty = bitmap.IsEmpty;

        Logger.Debug($"CreateTexture id={textureId} {width}x{height} empty={isEmpty} format={bitmap.Format}");

        // Determine format: Ultralight uses BGRA8 for color and A8 for masks
        var format = bitmap.Format == BitmapFormat.A8Unorm
            ? ImageFormat.R8G8B8A8_UNorm  // A8 textures → treat as R8 (we'll handle in shader)
            : ImageFormat.B8G8R8A8_UNorm;

        // RTT (render-to-texture) textures are empty bitmaps — used as color attachment targets
        var usage = isEmpty
            ? ImageUsage.ColorAttachment | ImageUsage.Sampled | ImageUsage.TransferDst
            : ImageUsage.Sampled | ImageUsage.TransferDst;

        var image = _gfx.CreateImage(new ImageDesc(new Extent2D(width, height), format, usage));
        var imageView = _gfx.CreateImageView(image);

        // If the bitmap has pixel data, upload it now
        if (!isEmpty)
        {
            var pixels = bitmap.LockPixels();
            if (pixels != null)
            {
                var pixelData = new ReadOnlySpan<byte>(pixels, (int)(bitmap.RowBytes * height));
                var bpp = bitmap.Format == BitmapFormat.A8Unorm ? 1 : 4;

                // Need tightly packed data for upload
                var packedRowBytes = width * (uint)bpp;
                if (bitmap.RowBytes == packedRowBytes)
                {
                    _gfx.UploadTexture2D(image, pixelData, width, height, bpp);
                }
                else
                {
                    // Strip row padding
                    var packed = new byte[packedRowBytes * height];
                    for (uint y = 0; y < height; y++)
                    {
                        pixelData.Slice((int)(y * bitmap.RowBytes), (int)packedRowBytes)
                            .CopyTo(packed.AsSpan((int)(y * packedRowBytes)));
                    }
                    _gfx.UploadTexture2D(image, packed, width, height, bpp);
                }
                bitmap.UnlockPixels();
            }
        }

        _textures[textureId] = new TextureEntry(image, imageView, width, height, format);
    }

    /// <inheritdoc />
    public unsafe void UpdateTexture(uint textureId, UlBitmap bitmap)
    {
        if (!_textures.TryGetValue(textureId, out var entry))
        {
            Logger.Warn($"UpdateTexture: unknown texture id={textureId}");
            return;
        }

        var width = bitmap.Width;
        var height = bitmap.Height;

        Logger.Debug($"UpdateTexture id={textureId} {width}x{height}");

        // If dimensions changed, recreate
        if (entry.Width != width || entry.Height != height)
        {
            DestroyTexture(textureId);
            CreateTexture(textureId, bitmap);
            return;
        }

        if (bitmap.IsEmpty)
            return;

        var pixels = bitmap.LockPixels();
        if (pixels == null)
            return;

        var bpp = bitmap.Format == BitmapFormat.A8Unorm ? 1 : 4;
        var pixelData = new ReadOnlySpan<byte>(pixels, (int)(bitmap.RowBytes * height));
        var packedRowBytes = width * (uint)bpp;

        if (bitmap.RowBytes == packedRowBytes)
        {
            _gfx.UploadTexture2D(entry.Image, pixelData, width, height, bpp);
        }
        else
        {
            var packed = new byte[packedRowBytes * height];
            for (uint y = 0; y < height; y++)
            {
                pixelData.Slice((int)(y * bitmap.RowBytes), (int)packedRowBytes)
                    .CopyTo(packed.AsSpan((int)(y * packedRowBytes)));
            }
            _gfx.UploadTexture2D(entry.Image, packed, width, height, bpp);
        }
        bitmap.UnlockPixels();
    }

    /// <inheritdoc />
    public void DestroyTexture(uint textureId)
    {
        if (!_textures.Remove(textureId, out var entry)) return;
        Logger.Debug($"DestroyTexture id={textureId}");
        entry.ImageView?.Dispose();
        entry.Image?.Dispose();
    }

    // ══════════════════════════════════════════════════════════════════
    //  Render Buffers
    // ══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public uint NextRenderBufferId() => _nextRenderBufferId++;

    /// <inheritdoc />
    public void CreateRenderBuffer(uint renderBufferId, UlRenderBuffer renderBuffer)
    {
        Logger.Debug($"CreateRenderBuffer id={renderBufferId} tex={renderBuffer.TextureId} " +
                     $"{renderBuffer.Width}x{renderBuffer.Height} stencil={renderBuffer.HasStencilBuffer}");

        if (!_textures.TryGetValue(renderBuffer.TextureId, out var texEntry))
        {
            Logger.Warn($"CreateRenderBuffer: texture id={renderBuffer.TextureId} not found!");
            return;
        }

        _renderBuffers[renderBufferId] = new RenderBufferEntry(
            renderBuffer.TextureId,
            texEntry.Image,
            texEntry.ImageView,
            renderBuffer.Width,
            renderBuffer.Height,
            renderBuffer.HasStencilBuffer);
    }

    /// <inheritdoc />
    public void DestroyRenderBuffer(uint renderBufferId)
    {
        if (!_renderBuffers.Remove(renderBufferId, out _)) return;
        Logger.Debug($"DestroyRenderBuffer id={renderBufferId}");
        // The texture is owned by the texture map, not the render buffer
    }

    // ══════════════════════════════════════════════════════════════════
    //  Geometry
    // ══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public uint NextGeometryId() => _nextGeometryId++;

    /// <inheritdoc />
    public unsafe void CreateGeometry(uint geometryId, UlVertexBuffer vertexBuffer, UlIndexBuffer indexBuffer)
    {
        Logger.Debug($"CreateGeometry id={geometryId} verts={vertexBuffer.Size}B indices={indexBuffer.Size}B format={vertexBuffer.Format}");

        var vb = _gfx.CreateBuffer(new BufferDesc(
            vertexBuffer.Size,
            BufferUsage.Vertex | BufferUsage.TransferDst,
            CpuAccessMode.Write));

        var ib = _gfx.CreateBuffer(new BufferDesc(
            indexBuffer.Size,
            BufferUsage.Index | BufferUsage.TransferDst,
            CpuAccessMode.Write));

        // Deep copy vertex data
        var vbSpan = _gfx.Map(vb);
        new ReadOnlySpan<byte>(vertexBuffer.Data, (int)vertexBuffer.Size).CopyTo(vbSpan);
        _gfx.Unmap(vb);

        // Deep copy index data
        var ibSpan = _gfx.Map(ib);
        new ReadOnlySpan<byte>(indexBuffer.Data, (int)indexBuffer.Size).CopyTo(ibSpan);
        _gfx.Unmap(ib);

        _geometries[geometryId] = new GeometryEntry(vb, ib, vertexBuffer.Format);
    }

    /// <inheritdoc />
    public unsafe void UpdateGeometry(uint geometryId, UlVertexBuffer vertexBuffer, UlIndexBuffer indexBuffer)
    {
        if (!_geometries.TryGetValue(geometryId, out var entry))
        {
            Logger.Warn($"UpdateGeometry: unknown geometry id={geometryId}");
            return;
        }

        Logger.Debug($"UpdateGeometry id={geometryId} verts={vertexBuffer.Size}B indices={indexBuffer.Size}B");

        // If sizes changed, recreate
        if ((ulong)vertexBuffer.Size > entry.VertexBuffer.Description.Size ||
            (ulong)indexBuffer.Size > entry.IndexBuffer.Description.Size)
        {
            DestroyGeometry(geometryId);
            CreateGeometry(geometryId, vertexBuffer, indexBuffer);
            return;
        }

        // Update in-place
        var vbSpan = _gfx.Map(entry.VertexBuffer);
        new ReadOnlySpan<byte>(vertexBuffer.Data, (int)vertexBuffer.Size).CopyTo(vbSpan);
        _gfx.Unmap(entry.VertexBuffer);

        var ibSpan = _gfx.Map(entry.IndexBuffer);
        new ReadOnlySpan<byte>(indexBuffer.Data, (int)indexBuffer.Size).CopyTo(ibSpan);
        _gfx.Unmap(entry.IndexBuffer);
    }

    /// <inheritdoc />
    public void DestroyGeometry(uint geometryId)
    {
        if (!_geometries.Remove(geometryId, out var entry)) return;
        Logger.Debug($"DestroyGeometry id={geometryId}");
        entry.VertexBuffer.Dispose();
        entry.IndexBuffer.Dispose();
    }

    // ══════════════════════════════════════════════════════════════════
    //  Command List
    // ══════════════════════════════════════════════════════════════════

    /// <inheritdoc />
    public void UpdateCommandList(UlCommandList commandList)
    {
        // Deep copy the command list — it won't persist beyond this call
        var commands = commandList.AsSpan();
        Logger.Debug($"UpdateCommandList: {commands.Length} commands");

        foreach (ref readonly var cmd in commands)
        {
            _pendingCommands.Add(new StoredCommand(cmd));
        }
    }

    // ══════════════════════════════════════════════════════════════════
    //  Command Execution (called by the render node)
    // ══════════════════════════════════════════════════════════════════

    /// <summary>
    /// Executes all pending Ultralight draw commands using the given command buffer
    /// and render pass context. Call this from the render node during the Vulkan
    /// render pass.
    /// </summary>
    /// <param name="commandBuffer">The active Vulkan command buffer.</param>
    /// <param name="renderPass">The active render pass (for pipeline creation).</param>
    public void FlushCommands(ICommandBuffer commandBuffer, IRenderPass renderPass)
    {
        if (_pendingCommands.Count == 0) return;

        EnsurePipelinesCreated(renderPass);

        foreach (var stored in _pendingCommands)
        {
            var cmd = stored;
            switch (cmd.CommandType)
            {
                case CommandType.ClearRenderBuffer:
                    // In the current architecture the main render pass is already active,
                    // so we handle clear as a no-op (Ultralight clears via the initial
                    // render buffer state). For full offscreen support this would need
                    // secondary render passes.
                    break;

                case CommandType.DrawGeometry:
                    ExecuteDrawGeometry(commandBuffer, cmd);
                    break;
            }
        }

        _pendingCommands.Clear();
    }

    private void ExecuteDrawGeometry(ICommandBuffer cmd, StoredCommand stored)
    {
        var state = stored.GpuState;

        if (!_geometries.TryGetValue(stored.GeometryId, out var geom))
        {
            Logger.Warn($"DrawGeometry: unknown geometry id={stored.GeometryId}");
            return;
        }

        // Select pipeline based on shader type
        var pipeline = state.ShaderType == ShaderType.FillPath ? _fillPathPipeline : _fillPipeline;
        if (pipeline is null) return;

        _gfx.BindGraphicsPipeline(cmd, pipeline);

        // Set viewport
        _gfx.SetViewport(cmd, 0, 0, state.ViewportWidth, state.ViewportHeight, 0, 1);

        // Set scissor
        if (state.EnableScissor)
        {
            var sr = state.ScissorRect;
            _gfx.SetScissor(cmd, sr.Left, sr.Top,
                (uint)Math.Max(0, sr.Right - sr.Left),
                (uint)Math.Max(0, sr.Bottom - sr.Top));
        }
        else
        {
            _gfx.SetScissor(cmd, 0, 0, state.ViewportWidth, state.ViewportHeight);
        }

        // Build and push uniform data
        var uniforms = BuildUniformData(state);
        _gfx.PushConstants(cmd, pipeline, ShaderStageFlags.All, 0,
            MemoryMarshal.AsBytes(new ReadOnlySpan<UltralightUniforms>(ref uniforms)));

        // Bind geometry
        _gfx.BindVertexBuffers(cmd, 0, new[] { geom.VertexBuffer }, new ulong[] { 0 });
        _gfx.BindIndexBuffer(cmd, geom.IndexBuffer, 0, IndexType.UInt32);

        // Bind textures via descriptor set (if textures referenced)
        if (state.Texture1Id != 0 && _textures.TryGetValue(state.Texture1Id, out var tex1))
        {
            var ds = _gfx.CreateDescriptorSet();
            var sampler = _gfx.CreateSampler(new SamplerDesc(
                SamplerFilter.Linear, SamplerFilter.Linear,
                SamplerAddressMode.ClampToEdge,
                SamplerAddressMode.ClampToEdge,
                SamplerAddressMode.ClampToEdge));
            var binding = new CombinedImageSamplerBinding(tex1.ImageView, sampler, 1);
            _gfx.UpdateDescriptorSet(ds, uniformBinding: null, binding);
            _gfx.BindDescriptorSet(cmd, pipeline, ds);
            // Note: in a production implementation, descriptor sets and samplers would
            // be pooled/cached rather than created per-draw. This works for correctness.
        }

        // Draw
        _gfx.DrawIndexed(cmd, stored.IndicesCount, 1, stored.IndicesOffset);
    }

    private static UltralightUniforms BuildUniformData(UlGpuState state)
    {
        var uniforms = new UltralightUniforms();
        uniforms.State = new Vector4(state.ViewportWidth, state.ViewportHeight, 1.0f, 0.0f);
        uniforms.Transform = state.Transform;

        // Pack 8 scalars into 2 vec4s
        var scalars = state.Scalar;
        uniforms.Scalar0 = new Vector4(
            scalars.Length > 0 ? scalars[0] : 0,
            scalars.Length > 1 ? scalars[1] : 0,
            scalars.Length > 2 ? scalars[2] : 0,
            scalars.Length > 3 ? scalars[3] : 0);
        uniforms.Scalar1 = new Vector4(
            scalars.Length > 4 ? scalars[4] : 0,
            scalars.Length > 5 ? scalars[5] : 0,
            scalars.Length > 6 ? scalars[6] : 0,
            scalars.Length > 7 ? scalars[7] : 0);

        // Copy vectors
        var vectors = state.Vector;
        for (int i = 0; i < 8 && i < vectors.Length; i++)
            uniforms.Vectors[i] = vectors[i];

        // Copy clip data
        uniforms.ClipSize = state.ClipSize;
        var clips = state.Clip;
        for (int i = 0; i < 8 && i < clips.Length; i++)
            uniforms.Clips[i] = clips[i];

        return uniforms;
    }

    // ══════════════════════════════════════════════════════════════════
    //  Pipeline Management
    // ══════════════════════════════════════════════════════════════════

    private void EnsurePipelinesCreated(IRenderPass renderPass)
    {
        if (_fillPipeline is not null) return;

        Logger.Info("Creating Ultralight GPU driver pipelines...");
        UltralightGpuShaders.EnsureLoaded();

        // ── Fill pipeline (Vbf2F4Ub2F2F28F) ─────────────────────────
        _fillVs = _gfx.CreateShader(new ShaderDesc(ShaderStage.Vertex, UltralightGpuShaders.FillVertex, "main"));
        _fillFs = _gfx.CreateShader(new ShaderDesc(ShaderStage.Fragment, UltralightGpuShaders.FillFragment, "main"));

        // Vertex layout for Vbf2F4Ub2F2F28F:
        // float2 pos, ubyte4 color, float2 tex, float2 obj, float4 x7 = 140 bytes stride
        var fillBindings = new[] { new VertexInputBindingDesc(0, 140) };
        var fillAttributes = new[]
        {
            new VertexInputAttributeDesc(0, 0, VertexFormat.Float2, 0),        // in_Position
            new VertexInputAttributeDesc(1, 0, VertexFormat.UNormR8G8B8A8, 8), // in_Color
            new VertexInputAttributeDesc(2, 0, VertexFormat.Float2, 12),       // in_TexCoord
            new VertexInputAttributeDesc(3, 0, VertexFormat.Float2, 20),       // in_ObjCoord
            new VertexInputAttributeDesc(4, 0, VertexFormat.Float4, 28),       // in_Data0
            new VertexInputAttributeDesc(5, 0, VertexFormat.Float4, 44),       // in_Data1
            new VertexInputAttributeDesc(6, 0, VertexFormat.Float4, 60),       // in_Data2
            new VertexInputAttributeDesc(7, 0, VertexFormat.Float4, 76),       // in_Data3
            new VertexInputAttributeDesc(8, 0, VertexFormat.Float4, 92),       // in_Data4
            new VertexInputAttributeDesc(9, 0, VertexFormat.Float4, 108),      // in_Data5
            new VertexInputAttributeDesc(10, 0, VertexFormat.Float4, 124),     // in_Data6
        };

        // Push constants for the uniform block
        var pushConstants = new[]
        {
            new PushConstantRange(ShaderStageFlags.All, 0, (uint)Unsafe.SizeOf<UltralightUniforms>())
        };

        _fillPipeline = _gfx.CreateGraphicsPipeline(new GraphicsPipelineDesc(
            renderPass, _fillVs, _fillFs,
            BlendEnabled: true,
            CullBackFace: false,
            VertexBindings: fillBindings,
            VertexAttributes: fillAttributes,
            PushConstantRanges: pushConstants));

        // ── FillPath pipeline (Vbf2F4Ub2F) ──────────────────────────
        _fillPathVs = _gfx.CreateShader(new ShaderDesc(ShaderStage.Vertex, UltralightGpuShaders.FillPathVertex, "main"));
        _fillPathFs = _gfx.CreateShader(new ShaderDesc(ShaderStage.Fragment, UltralightGpuShaders.FillPathFragment, "main"));

        // Vertex layout for Vbf2F4Ub2F: float2 pos, ubyte4 color, float2 tex = 20 bytes stride
        var pathBindings = new[] { new VertexInputBindingDesc(0, 20) };
        var pathAttributes = new[]
        {
            new VertexInputAttributeDesc(0, 0, VertexFormat.Float2, 0),        // in_Position
            new VertexInputAttributeDesc(1, 0, VertexFormat.UNormR8G8B8A8, 8), // in_Color
            new VertexInputAttributeDesc(2, 0, VertexFormat.Float2, 12),       // in_TexCoord
        };

        _fillPathPipeline = _gfx.CreateGraphicsPipeline(new GraphicsPipelineDesc(
            renderPass, _fillPathVs, _fillPathFs,
            BlendEnabled: true,
            CullBackFace: false,
            VertexBindings: pathBindings,
            VertexAttributes: pathAttributes,
            PushConstantRanges: pushConstants));

        Logger.Info("Ultralight GPU driver pipelines created.");
    }

    // ══════════════════════════════════════════════════════════════════
    //  Disposal
    // ══════════════════════════════════════════════════════════════════

    public void Dispose()
    {
        Logger.Info("Disposing VulkanUltralightGpuDriver...");

        _pendingCommands.Clear();

        foreach (var entry in _geometries.Values)
        {
            entry.VertexBuffer.Dispose();
            entry.IndexBuffer.Dispose();
        }
        _geometries.Clear();

        // Render buffers don't own their textures
        _renderBuffers.Clear();

        foreach (var entry in _textures.Values)
        {
            entry.ImageView?.Dispose();
            entry.Image?.Dispose();
        }
        _textures.Clear();

        _fillPipeline = null; // Pipeline lifetime managed by GFX device
        _fillPathPipeline = null;
        _fillVs?.Dispose();
        _fillFs?.Dispose();
        _fillPathVs?.Dispose();
        _fillPathFs?.Dispose();

        Logger.Info("VulkanUltralightGpuDriver disposed.");
    }

    // ══════════════════════════════════════════════════════════════════
    //  Internal Types
    // ══════════════════════════════════════════════════════════════════

    private sealed record TextureEntry(
        IImage Image,
        IImageView ImageView,
        uint Width,
        uint Height,
        ImageFormat Format);

    private sealed record RenderBufferEntry(
        uint TextureId,
        IImage Image,
        IImageView ImageView,
        uint Width,
        uint Height,
        bool HasStencilBuffer);

    private sealed record GeometryEntry(
        IBuffer VertexBuffer,
        IBuffer IndexBuffer,
        VertexBufferFormat Format);

    /// <summary>
    /// Deep-copied representation of a single Ultralight GPU command,
    /// stored between <see cref="UpdateCommandList"/> and <see cref="FlushCommands"/>.
    /// </summary>
    private struct StoredCommand
    {
        public CommandType CommandType;
        public UlGpuState GpuState;
        public uint GeometryId;
        public uint IndicesCount;
        public uint IndicesOffset;

        public StoredCommand(in UlCommand cmd)
        {
            CommandType = cmd.CommandType;
            GpuState = cmd.GpuState;
            GeometryId = cmd.GeometryId;
            IndicesCount = cmd.IndicesCount;
            IndicesOffset = cmd.IndicesOffset;
        }
    }
}

/// <summary>
/// Uniform data block matching the Ultralight GPU shader uniform layout.
/// Passed as push constants to the Fill and FillPath pipelines.
/// </summary>
[StructLayout(LayoutKind.Sequential)]
internal struct UltralightUniforms
{
    public Vector4 State;           // viewport_w, viewport_h, scale, unused
    public Matrix4x4 Transform;    // 4x4 transform matrix

    // 8 scalars packed as 2 vec4s
    public Vector4 Scalar0;
    public Vector4 Scalar1;

    // 8 vec4 vectors
    public Vectors8 Vectors;

    // Clip data
    public uint ClipSize;
    public uint _pad0;
    public uint _pad1;
    public uint _pad2;
    public Clips8 Clips;
}

[InlineArray(8)]
internal struct Vectors8
{
    private Vector4 _element;
}

[InlineArray(8)]
internal struct Clips8
{
    private Matrix4x4 _element;
}




