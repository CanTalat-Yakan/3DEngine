namespace Engine;

/// <summary>
/// Render graph node that draws the Ultralight webview surface as a fullscreen textured overlay.
/// Uses CPU bitmap surface mode — reads pixels from Ultralight, uploads to a Vulkan texture,
/// then draws a fullscreen triangle with alpha blending.
/// Depends on "sample" so it renders after the 3D scene but before ImGui.
/// </summary>
public sealed class WebViewRenderNode : IRenderNode, IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.Vulkan");

    public string Name => "webview";
    public IReadOnlyCollection<string> Dependencies { get; } = new[] { "sample" };

    // GPU resources — created lazily on first Execute
    private IPipeline? _pipeline;
    private IShader? _vertexShader;
    private IShader? _fragmentShader;
    private IImage? _webviewImage;
    private IImageView? _webviewImageView;
    private ISampler? _sampler;
    private IDescriptorSet? _descriptorSet;

    // Tracked texture dimensions for resize detection
    private uint _textureWidth;
    private uint _textureHeight;

    // Staging buffer for pixel upload
    private byte[]? _stagingBuffer;

    public unsafe void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        var webview = renderWorld.TryGet<WebViewInstance>();
        if (webview?.View is null)
            return;

        var gfx = ctx.Graphics;
        var cmd = cmds.FrameContext.CommandBuffer;
        var extent = cmds.FrameContext.Extent;

        // Lazy-init pipeline
        if (_pipeline is null)
            CreatePipeline(gfx, cmds.FrameContext.RenderPass);

        // Recreate texture if webview size changed
        if (_webviewImage is null || _textureWidth != webview.Width || _textureHeight != webview.Height)
            RecreatewebviewTexture(gfx, webview.Width, webview.Height);

        // Upload new pixels if dirty
        if (webview.IsDirty && _webviewImage is not null)
        {
            var surfaceRowBytes = webview.SurfaceRowBytes;
            var totalBytes = (int)(surfaceRowBytes * webview.Height);

            // Ensure staging buffer is large enough for the surface (includes row padding)
            if (_stagingBuffer is null || _stagingBuffer.Length < totalBytes)
                _stagingBuffer = new byte[totalBytes];

            if (webview.TryGetPixels(_stagingBuffer.AsSpan(0, totalBytes), out var actualRowBytes))
            {
                webview.DiagUploadCount++;
                var packedRowBytes = webview.Width * 4;

                if (actualRowBytes == packedRowBytes)
                {
                    // No padding — upload the surface data directly
                    gfx.UploadTexture2D(_webviewImage, _stagingBuffer.AsSpan(0, totalBytes),
                        webview.Width, webview.Height, bytesPerPixel: 4);
                }
                else
                {
                    // Surface has row padding — strip it to get tightly-packed rows
                    var packedSize = (int)(packedRowBytes * webview.Height);
                    var packed = new byte[packedSize];
                    for (uint y = 0; y < webview.Height; y++)
                    {
                        _stagingBuffer.AsSpan((int)(y * actualRowBytes), (int)packedRowBytes)
                            .CopyTo(packed.AsSpan((int)(y * packedRowBytes)));
                    }
                    gfx.UploadTexture2D(_webviewImage, packed.AsSpan(0, packedSize),
                        webview.Width, webview.Height, bytesPerPixel: 4);
                }
            }
        }

        if (_webviewImage is null || _pipeline is null || _descriptorSet is null)
            return;

        // Set viewport and scissor to full framebuffer
        gfx.SetViewport(cmd, 0, 0, extent.Width, extent.Height, 0, 1);
        gfx.SetScissor(cmd, 0, 0, extent.Width, extent.Height);

        // Bind pipeline and descriptor set (contains the webview texture)
        gfx.BindGraphicsPipeline(cmd, _pipeline);
        gfx.BindDescriptorSet(cmd, _pipeline, _descriptorSet);

        // Draw fullscreen triangle (3 vertices, generated in vertex shader)
        gfx.Draw(cmd, vertexCount: 3, instanceCount: 1);
    }

    private void CreatePipeline(IGraphicsDevice gfx, IRenderPass renderPass)
    {
        Logger.Info("Creating webview overlay Vulkan pipeline...");

        WebViewShaders.EnsureLoaded();
        var vsDesc = new ShaderDesc(ShaderStage.Vertex, WebViewShaders.Vertex, "main");
        var fsDesc = new ShaderDesc(ShaderStage.Fragment, WebViewShaders.Fragment, "main");
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
        Logger.Info("WebView overlay pipeline created.");
    }

    private void RecreatewebviewTexture(IGraphicsDevice gfx, uint width, uint height)
    {
        if (width == 0 || height == 0) return;

        Logger.Info($"Creating webview texture {width}x{height} B8G8R8A8_UNorm...");

        // Dispose old resources
        _descriptorSet?.Dispose();
        _sampler?.Dispose();
        _webviewImageView?.Dispose();
        _webviewImage?.Dispose();

        var imageDesc = new ImageDesc(
            new Extent2D(width, height),
            ImageFormat.B8G8R8A8_UNorm,
            ImageUsage.Sampled | ImageUsage.TransferDst);

        _webviewImage = gfx.CreateImage(imageDesc);
        _webviewImageView = gfx.CreateImageView(_webviewImage);
        _sampler = gfx.CreateSampler(new SamplerDesc(
            SamplerFilter.Linear, SamplerFilter.Linear,
            SamplerAddressMode.ClampToEdge,
            SamplerAddressMode.ClampToEdge,
            SamplerAddressMode.ClampToEdge));

        _descriptorSet = gfx.CreateDescriptorSet();
        var samplerBinding = new CombinedImageSamplerBinding(_webviewImageView, _sampler, 1);
        gfx.UpdateDescriptorSet(_descriptorSet, uniformBinding: null, samplerBinding);

        _textureWidth = width;
        _textureHeight = height;

        // Clear the staging buffer to ensure a clean first upload
        _stagingBuffer = null;

        Logger.Info($"WebView texture created: {width}x{height}.");
    }

    public void Dispose()
    {
        Logger.Info("Disposing WebViewRenderNode GPU resources...");
        _descriptorSet?.Dispose();
        _sampler?.Dispose();
        _webviewImageView?.Dispose();
        _webviewImage?.Dispose();
        _fragmentShader?.Dispose();
        _vertexShader?.Dispose();
        _stagingBuffer = null;
    }
}


