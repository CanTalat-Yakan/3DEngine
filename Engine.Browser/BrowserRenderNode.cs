namespace Engine;

/// <summary>
/// Render graph node that draws the Ultralight browser surface as a fullscreen textured overlay.
/// Uses CPU bitmap surface mode — reads pixels from Ultralight, uploads to a Vulkan texture,
/// then draws a fullscreen triangle with alpha blending.
/// Depends on "sample" so it renders after the 3D scene but before ImGui.
/// </summary>
public sealed class BrowserRenderNode : IRenderNode, IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.Browser.Vulkan");

    public string Name => "browser";
    public IReadOnlyCollection<string> Dependencies { get; } = new[] { "sample" };

    // GPU resources — created lazily on first Execute
    private IPipeline? _pipeline;
    private IShader? _vertexShader;
    private IShader? _fragmentShader;
    private IImage? _browserImage;
    private IImageView? _browserImageView;
    private ISampler? _sampler;
    private IDescriptorSet? _descriptorSet;

    // Tracked texture dimensions for resize detection
    private uint _textureWidth;
    private uint _textureHeight;

    // Staging buffer for pixel upload
    private byte[]? _stagingBuffer;

    public unsafe void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        var browser = renderWorld.TryGet<BrowserInstance>();
        if (browser?.View is null)
            return;

        var gfx = ctx.Graphics;
        var cmd = cmds.FrameContext.CommandBuffer;
        var extent = cmds.FrameContext.Extent;

        // Lazy-init pipeline
        if (_pipeline is null)
            CreatePipeline(gfx, cmds.FrameContext.RenderPass);

        // Recreate texture if browser size changed
        if (_browserImage is null || _textureWidth != browser.Width || _textureHeight != browser.Height)
            RecreateBrowserTexture(gfx, browser.Width, browser.Height);

        // Upload new pixels if dirty
        if (browser.IsDirty && _browserImage is not null)
        {
            var surfaceRowBytes = browser.SurfaceRowBytes;
            var totalBytes = (int)(surfaceRowBytes * browser.Height);

            // Ensure staging buffer is large enough for the surface (includes row padding)
            if (_stagingBuffer is null || _stagingBuffer.Length < totalBytes)
                _stagingBuffer = new byte[totalBytes];

            if (browser.TryGetPixels(_stagingBuffer.AsSpan(0, totalBytes), out var actualRowBytes))
            {
                browser.DiagUploadCount++;
                var packedRowBytes = browser.Width * 4;

                if (actualRowBytes == packedRowBytes)
                {
                    // No padding — upload the surface data directly
                    gfx.UploadTexture2D(_browserImage, _stagingBuffer.AsSpan(0, totalBytes),
                        browser.Width, browser.Height, bytesPerPixel: 4);
                }
                else
                {
                    // Surface has row padding — strip it to get tightly-packed rows
                    var packedSize = (int)(packedRowBytes * browser.Height);
                    var packed = new byte[packedSize];
                    for (uint y = 0; y < browser.Height; y++)
                    {
                        _stagingBuffer.AsSpan((int)(y * actualRowBytes), (int)packedRowBytes)
                            .CopyTo(packed.AsSpan((int)(y * packedRowBytes)));
                    }
                    gfx.UploadTexture2D(_browserImage, packed.AsSpan(0, packedSize),
                        browser.Width, browser.Height, bytesPerPixel: 4);
                }
            }
        }

        if (_browserImage is null || _pipeline is null || _descriptorSet is null)
            return;

        // Set viewport and scissor to full framebuffer
        gfx.SetViewport(cmd, 0, 0, extent.Width, extent.Height, 0, 1);
        gfx.SetScissor(cmd, 0, 0, extent.Width, extent.Height);

        // Bind pipeline and descriptor set (contains the browser texture)
        gfx.BindGraphicsPipeline(cmd, _pipeline);
        gfx.BindDescriptorSet(cmd, _pipeline, _descriptorSet);

        // Draw fullscreen triangle (3 vertices, generated in vertex shader)
        gfx.Draw(cmd, vertexCount: 3, instanceCount: 1);
    }

    private void CreatePipeline(IGraphicsDevice gfx, IRenderPass renderPass)
    {
        Logger.Info("Creating browser overlay Vulkan pipeline...");

        BrowserShaders.EnsureLoaded();
        var vsDesc = new ShaderDesc(ShaderStage.Vertex, BrowserShaders.Vertex, "main");
        var fsDesc = new ShaderDesc(ShaderStage.Fragment, BrowserShaders.Fragment, "main");
        _vertexShader = gfx.CreateShader(vsDesc);
        _fragmentShader = gfx.CreateShader(fsDesc);

        // Fullscreen triangle: no vertex input needed
        var pipelineDesc = new GraphicsPipelineDesc(
            renderPass,
            _vertexShader,
            _fragmentShader,
            BlendEnabled: true,      // alpha composite over the scene
            CullBackFace: false,     // fullscreen triangle winding may vary
            VertexBindings: null,
            VertexAttributes: null,
            PushConstantRanges: null);

        _pipeline = gfx.CreateGraphicsPipeline(pipelineDesc);
        Logger.Info("Browser overlay pipeline created.");
    }

    private void RecreateBrowserTexture(IGraphicsDevice gfx, uint width, uint height)
    {
        if (width == 0 || height == 0) return;

        Logger.Info($"Creating browser texture {width}x{height} B8G8R8A8_UNorm...");

        // Dispose old resources
        _descriptorSet?.Dispose();
        _sampler?.Dispose();
        _browserImageView?.Dispose();
        _browserImage?.Dispose();

        var imageDesc = new ImageDesc(
            new Extent2D(width, height),
            ImageFormat.B8G8R8A8_UNorm,
            ImageUsage.Sampled | ImageUsage.TransferDst);

        _browserImage = gfx.CreateImage(imageDesc);
        _browserImageView = gfx.CreateImageView(_browserImage);
        _sampler = gfx.CreateSampler(new SamplerDesc(
            SamplerFilter.Linear, SamplerFilter.Linear,
            SamplerAddressMode.ClampToEdge,
            SamplerAddressMode.ClampToEdge,
            SamplerAddressMode.ClampToEdge));

        _descriptorSet = gfx.CreateDescriptorSet();
        var samplerBinding = new CombinedImageSamplerBinding(_browserImageView, _sampler, 1);
        gfx.UpdateDescriptorSet(_descriptorSet, uniformBinding: null, samplerBinding);

        _textureWidth = width;
        _textureHeight = height;

        // Clear the staging buffer to ensure a clean first upload
        _stagingBuffer = null;

        Logger.Info($"Browser texture created: {width}x{height}.");
    }

    public void Dispose()
    {
        Logger.Info("Disposing BrowserRenderNode GPU resources...");
        _descriptorSet?.Dispose();
        _sampler?.Dispose();
        _browserImageView?.Dispose();
        _browserImage?.Dispose();
        _fragmentShader?.Dispose();
        _vertexShader?.Dispose();
        _stagingBuffer = null;
    }
}


