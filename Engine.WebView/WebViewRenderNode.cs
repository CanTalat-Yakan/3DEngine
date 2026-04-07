namespace Engine;

/// <summary>
/// Render graph node that draws the Ultralight webview surface as a fullscreen textured overlay.
/// Uses CPU bitmap surface mode - reads pixels from Ultralight, uploads to a Vulkan texture,
/// then draws a fullscreen triangle with alpha blending.
/// Depends on "sample" so it renders after the 3D scene but before ImGui.
/// </summary>
/// <remarks>
/// <para>
/// GPU resources (pipeline, shaders, texture, sampler, descriptor set) are created lazily on
/// first <see cref="Execute"/>.  The texture is recreated whenever the webview dimensions change.
/// A <em>quiescent resize guard</em> skips surface access on the frame where a native resize
/// was committed, drawing the previous frame's texture instead.
/// </para>
/// </remarks>
/// <seealso cref="WebViewInstance"/>
/// <seealso cref="WebViewShaders"/>
/// <seealso cref="WebViewPlugin"/>
/// <seealso cref="VulkanWebViewPlugin"/>
public sealed class WebViewRenderNode : IRenderNode, IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.Vulkan");

    /// <inheritdoc />
    public string Name => "webview";
    /// <inheritdoc />
    public IReadOnlyCollection<string> Dependencies { get; } = new[] { "sample" };

    // GPU resources - created lazily on first Execute
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

    // Tracks the webview resize generation to skip pixel access on the
    // frame where a resize was committed (surface just reallocated, not
    // yet painted into by Ultralight).
    private uint _lastSeenResizeGeneration;

    // Staging buffer for pixel upload
    private byte[]? _stagingBuffer;

    /// <inheritdoc />
    /// <param name="ctx">The current renderer context providing GPU device access.</param>
    /// <param name="cmds">The command recording context for the current frame.</param>
    /// <param name="renderWorld">The render world containing the <see cref="WebViewInstance"/> resource.</param>
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

        // ── Quiescent resize guard ──────────────────────────────────
        // On the frame where a native ulViewResize was committed, the
        // surface has been reallocated but Ultralight has NOT yet painted
        // into it (Update/Render were skipped).  We must not touch the
        // surface at all - just draw the previous frame's texture.
        var currentGen = webview.ResizeGeneration;
        if (currentGen != _lastSeenResizeGeneration)
        {
            // Generation changed - a resize happened.  Skip surface
            // access this frame and defer texture recreation to the
            // next frame when the surface has been painted into.
            _lastSeenResizeGeneration = currentGen;
            goto draw;
        }

        // Recreate texture if webview size changed
        if (_webviewImage is null || _textureWidth != webview.Width || _textureHeight != webview.Height)
            RecreatewebviewTexture(gfx, webview.Width, webview.Height);

        // Upload new pixels if dirty
        if (webview.IsDirty && _webviewImage is not null)
        {
            var surfaceRowBytes = webview.SurfaceRowBytes;
            var currentWidth = webview.Width;
            var currentHeight = webview.Height;
            var totalBytes = (int)(surfaceRowBytes * currentHeight);

            // Guard: skip upload if dimensions are zero or texture size doesn't match
            if (totalBytes <= 0 || _textureWidth != currentWidth || _textureHeight != currentHeight)
                goto draw;

            // Ensure staging buffer is large enough for the surface (includes row padding)
            if (_stagingBuffer is null || _stagingBuffer.Length < totalBytes)
                _stagingBuffer = new byte[totalBytes];

            if (webview.TryGetPixels(_stagingBuffer.AsSpan(0, totalBytes), out var actualRowBytes))
            {
                webview.DiagUploadCount++;
                var packedRowBytes = currentWidth * 4;

                if (actualRowBytes == packedRowBytes)
                {
                    // No padding - upload the surface data directly
                    gfx.UploadTexture2D(_webviewImage, _stagingBuffer.AsSpan(0, totalBytes),
                        currentWidth, currentHeight, bytesPerPixel: 4);
                }
                else
                {
                    // Surface has row padding - strip it to get tightly-packed rows
                    var packedSize = (int)(packedRowBytes * currentHeight);
                    var packed = new byte[packedSize];
                    for (uint y = 0; y < currentHeight; y++)
                    {
                        _stagingBuffer.AsSpan((int)(y * actualRowBytes), (int)packedRowBytes)
                            .CopyTo(packed.AsSpan((int)(y * packedRowBytes)));
                    }
                    gfx.UploadTexture2D(_webviewImage, packed.AsSpan(0, packedSize),
                        currentWidth, currentHeight, bytesPerPixel: 4);
                }
            }
        }

        draw:

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

    /// <summary>Creates the fullscreen overlay Vulkan pipeline with alpha blending and no vertex input.</summary>
    /// <param name="gfx">The graphics device to create GPU resources on.</param>
    /// <param name="renderPass">The render pass the pipeline must be compatible with.</param>
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

    /// <summary>Recreates the GPU texture, image view, sampler, and descriptor set for the given dimensions.</summary>
    /// <param name="gfx">The graphics device to create GPU resources on.</param>
    /// <param name="width">New texture width in pixels.</param>
    /// <param name="height">New texture height in pixels.</param>
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

    /// <summary>Disposes all GPU resources (pipeline, shaders, texture, sampler, descriptor set).</summary>
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


