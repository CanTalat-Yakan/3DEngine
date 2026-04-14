namespace Engine;

/// <summary>
/// Render graph node that draws the Ultralight webview surface as a fullscreen textured overlay.
/// Reads pixels from the CPU bitmap surface, uploads to a Vulkan texture every frame,
/// then draws a fullscreen triangle with alpha blending into the shared
/// <see cref="ActiveSwapchainPass"/>.
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

    // Tracks the webview resize generation to skip reads on reallocation frames
    private uint _lastSeenResizeGeneration;

    // Staging buffer for pixel upload
    private byte[]? _stagingBuffer;

    /// <summary>Creates a new <see cref="WebViewRenderNode"/> with pre-compiled shader SPIR-V bytecode.</summary>
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

        var activePass = renderWorld.TryGet<ActiveSwapchainPass>();
        if (activePass is null) return;

        var gfx = renderContext.Device;
        var swapchainTarget = renderWorld.TryGet<SwapchainTarget>();
        if (swapchainTarget is null) return;

        if (_pipeline is null)
            CreatePipeline(gfx, swapchainTarget.RenderPass);

        // ── Quiescent resize guard ──────────────────────────────────
        // On the frame a native resize was committed the surface is freshly
        // allocated and empty - skip the upload, just draw the old texture.
        var currentGen = webview.ResizeGeneration;
        if (currentGen != _lastSeenResizeGeneration)
        {
            _lastSeenResizeGeneration = currentGen;
        }
        else
        {
            // Recreate GPU texture if dimensions changed
            if (_webviewImage is null || _textureWidth != webview.Width || _textureHeight != webview.Height)
                RecreateWebviewTexture(gfx, webview.Width, webview.Height);

            // Upload pixels every frame
            if (_webviewImage is not null)
                activePass = UploadPixels(webview, activePass, gfx, renderContext, swapchainTarget, renderWorld);
        }

        // ── Draw ────────────────────────────────────────────────────
        if (_webviewImage is null || _pipeline is null || _descriptorSet is null)
            return;

        var pass = activePass.Pass;
        pass.SetPipeline(_pipeline);
        pass.SetBindGroup(_pipeline, _descriptorSet);
        pass.Draw(vertexCount: 3, instanceCount: 1);
    }

    /// <summary>
    /// Reads pixels from the webview surface and uploads them to the GPU texture.
    /// Interrupts and re-opens the active render pass for the transfer command.
    /// </summary>
    private ActiveSwapchainPass UploadPixels(
        WebViewInstance webview,
        ActiveSwapchainPass activePass,
        IGraphicsDevice gfx,
        RenderContext renderContext,
        SwapchainTarget swapchainTarget,
        RenderWorld renderWorld)
    {
        var surfaceRowBytes = webview.SurfaceRowBytes;
        var currentWidth = webview.Width;
        var currentHeight = webview.Height;
        var totalBytes = (int)(surfaceRowBytes * currentHeight);

        if (totalBytes <= 0 || _textureWidth != currentWidth || _textureHeight != currentHeight)
            return activePass;

        if (_stagingBuffer is null || _stagingBuffer.Length < totalBytes)
            _stagingBuffer = new byte[totalBytes];

        if (!webview.TryGetPixels(_stagingBuffer.AsSpan(0, totalBytes), out var actualRowBytes))
            return activePass;

        webview.DiagUploadCount++;
        var packedRowBytes = currentWidth * 4;

        // End the render pass - vkCmdCopyBufferToImage cannot be inside one.
        activePass.Pass.EndRenderPass();

        var cmd = renderContext.CommandBuffer;

        if (actualRowBytes == packedRowBytes)
        {
            gfx.UploadTexture2DDeferred(cmd, _webviewImage!, _stagingBuffer.AsSpan(0, totalBytes),
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
            gfx.UploadTexture2DDeferred(cmd, _webviewImage!, packed.AsSpan(0, packedSize),
                currentWidth, currentHeight, bytesPerPixel: 4);
        }

        // Re-open the render pass with Load to preserve existing content.
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
        var updated = new ActiveSwapchainPass(newPass, extent);
        renderWorld.Set(updated);
        return updated;
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
