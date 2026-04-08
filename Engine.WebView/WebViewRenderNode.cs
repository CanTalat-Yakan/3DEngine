namespace Engine;

/// <summary>
/// Render graph node that draws the Ultralight webview surface as a fullscreen textured overlay.
/// Uses CPU bitmap surface mode - reads pixels from Ultralight, uploads to a Vulkan texture,
/// then draws a fullscreen triangle with alpha blending into the shared
/// <see cref="ActiveSwapchainPass"/> (no separate render pass begin/end).
/// </summary>
/// <seealso cref="WebViewInstance"/>
/// <seealso cref="WebViewPlugin"/>
/// <seealso cref="VulkanWebViewPlugin"/>
public sealed class WebViewRenderNode : INode, IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.Vulkan");

    private readonly ReadOnlyMemory<byte> _vertexSpv;
    private readonly ReadOnlyMemory<byte> _fragmentSpv;

    // GPU resources - created lazily on first Run
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

    // Tracks the webview resize generation
    private uint _lastSeenResizeGeneration;

    // Staging buffer for pixel upload
    private byte[]? _stagingBuffer;

    /// <summary>Creates a new <see cref="WebViewRenderNode"/> with pre-compiled shader SPIR-V bytecode.</summary>
    /// <param name="vertexSpv">Compiled SPIR-V bytecode for the webview vertex shader.</param>
    /// <param name="fragmentSpv">Compiled SPIR-V bytecode for the webview fragment shader.</param>
    public WebViewRenderNode(ReadOnlyMemory<byte> vertexSpv, ReadOnlyMemory<byte> fragmentSpv)
    {
        _vertexSpv = vertexSpv;
        _fragmentSpv = fragmentSpv;
    }

    /// <inheritdoc />
    public void Run(RenderGraphContext graphContext, RenderContext renderContext, RenderWorld renderWorld)
    {
        var webview = renderWorld.TryGet<WebViewInstance>();
        if (webview?.View is null || !webview.Visible)
            return;

        // Get the shared swapchain pass opened by MainPassNode
        var activePass = renderWorld.TryGet<ActiveSwapchainPass>();
        if (activePass is null) return;

        var gfx = renderContext.Device;
        var swapchainTarget = renderWorld.TryGet<SwapchainTarget>();
        if (swapchainTarget is null) return;

        // Lazy-init pipeline (created against the main render pass for compatibility)
        if (_pipeline is null)
            CreatePipeline(gfx, swapchainTarget.RenderPass);

        // ── Quiescent resize guard ──────────────────────────────────
        var currentGen = webview.ResizeGeneration;
        if (currentGen != _lastSeenResizeGeneration)
        {
            _lastSeenResizeGeneration = currentGen;
            goto draw;
        }

        // Recreate texture if webview size changed
        if (_webviewImage is null || _textureWidth != webview.Width || _textureHeight != webview.Height)
            RecreateWebviewTexture(gfx, webview.Width, webview.Height);

        // Upload new pixels if dirty
        if (webview.IsDirty && _webviewImage is not null)
        {
            var surfaceRowBytes = webview.SurfaceRowBytes;
            var currentWidth = webview.Width;
            var currentHeight = webview.Height;
            var totalBytes = (int)(surfaceRowBytes * currentHeight);

            if (totalBytes <= 0 || _textureWidth != currentWidth || _textureHeight != currentHeight)
                goto draw;

            if (_stagingBuffer is null || _stagingBuffer.Length < totalBytes)
                _stagingBuffer = new byte[totalBytes];

            if (webview.TryGetPixels(_stagingBuffer.AsSpan(0, totalBytes), out var actualRowBytes))
            {
                webview.DiagUploadCount++;
                var packedRowBytes = currentWidth * 4;

                // End the render pass before texture upload (vkCmdCopyBufferToImage
                // cannot be recorded inside a render pass). MainPassNode's pass will
                // be re-opened below.
                activePass.Pass.EndRenderPass();

                // Use deferred upload: records copy commands into the frame command buffer
                // and tracks the staging buffer for cleanup after the frame fence signals.
                // No GPU stall (no vkWaitForFences per upload).
                var cmd = renderContext.CommandBuffer;

                if (actualRowBytes == packedRowBytes)
                {
                    gfx.UploadTexture2DDeferred(cmd, _webviewImage, _stagingBuffer.AsSpan(0, totalBytes),
                        currentWidth, currentHeight, bytesPerPixel: 4);
                }
                else
                {
                    var packedSize = (int)(packedRowBytes * currentHeight);
                    var packed = new byte[packedSize];
                    for (uint y = 0; y < currentHeight; y++)
                    {
                        _stagingBuffer.AsSpan((int)(y * actualRowBytes), (int)packedRowBytes)
                            .CopyTo(packed.AsSpan((int)(y * packedRowBytes)));
                    }
                    gfx.UploadTexture2DDeferred(cmd, _webviewImage, packed.AsSpan(0, packedSize),
                        currentWidth, currentHeight, bytesPerPixel: 4);
                }

                // Re-open the render pass with Load to preserve the cleared content
                var reloadDesc = new RenderPassDescriptor(
                    swapchainTarget.LoadRenderPass,
                    swapchainTarget.Framebuffer,
                    swapchainTarget.Extent,
                    LoadOp.Load,
                    StoreOp.Store);
                var newPass = renderContext.BeginTrackedRenderPass(reloadDesc);
                var extent = activePass.Extent;
                newPass.SetViewport(0, 0, extent.Width, extent.Height, 0, 1);
                newPass.SetScissor(0, 0, extent.Width, extent.Height);
                renderWorld.Set(new ActiveSwapchainPass(newPass, extent));
                activePass = renderWorld.TryGet<ActiveSwapchainPass>()!;
            }
        }

        draw:

        if (_webviewImage is null || _pipeline is null || _descriptorSet is null)
            return;

        // Draw into the shared pass - no separate render pass needed
        var pass = activePass.Pass;

        // Bind pipeline and descriptor set (contains the webview texture)
        pass.SetPipeline(_pipeline);
        pass.SetBindGroup(_pipeline, _descriptorSet);

        // Draw fullscreen triangle (3 vertices, generated in vertex shader)
        pass.Draw(vertexCount: 3, instanceCount: 1);
    }

    /// <summary>Creates the fullscreen overlay Vulkan pipeline with alpha blending and no vertex input.</summary>
    private void CreatePipeline(IGraphicsDevice gfx, IRenderPass renderPass)
    {
        Logger.Info("Creating webview overlay Vulkan pipeline...");

        var vsDesc = new ShaderDesc(ShaderStage.Vertex, _vertexSpv, "main");
        var fsDesc = new ShaderDesc(ShaderStage.Fragment, _fragmentSpv, "main");
        _vertexShader = gfx.CreateShader(vsDesc);
        _fragmentShader = gfx.CreateShader(fsDesc);

        var pipelineDesc = new GraphicsPipelineDesc(
            renderPass,
            _vertexShader,
            _fragmentShader,
            BlendEnabled: true,
            CullBackFace: false,
            VertexBindings: null,
            VertexAttributes: null,
            PushConstantRanges: null);

        _pipeline = gfx.CreateGraphicsPipeline(pipelineDesc);
        Logger.Info("WebView overlay pipeline created.");
    }

    /// <summary>Recreates the GPU texture, image view, sampler, and descriptor set for the given dimensions.</summary>
    private void RecreateWebviewTexture(IGraphicsDevice gfx, uint width, uint height)
    {
        if (width == 0 || height == 0) return;

        Logger.Info($"Creating webview texture {width}x{height} B8G8R8A8_UNorm...");

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
        _stagingBuffer = null;

        Logger.Info($"WebView texture created: {width}x{height}.");
    }

    /// <summary>Disposes all GPU resources.</summary>
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
