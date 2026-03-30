namespace Engine;

/// <summary>
/// Plugin that uses UltralightNet to render a web page (e.g., Editor.Server Blazor app)
/// into a texture and composites it as a full-screen overlay on the Vulkan renderer.
/// Modeled after <see cref="SdlImGuiPlugin"/> — registers systems for update, render, and cleanup.
/// </summary>
public sealed class UltralightPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Editor.Ultralight");

    private readonly string _url;

    /// <summary>
    /// Creates the plugin with the URL of the web app to display.
    /// </summary>
    /// <param name="url">URL to load in the Ultralight view (e.g., "http://localhost:5000").</param>
    public UltralightPlugin(string url = "http://localhost:5000")
    {
        _url = url;
    }

    public void Build(App app)
    {
        Logger.Info($"UltralightPlugin: Building — target URL: {_url}");

        // ---- GPU resources (lazily created once the renderer is initialized) ----
        IImage? gpuImage = null;
        IImageView? gpuImageView = null;
        ISampler? gpuSampler = null;
        IDescriptorSet? descriptorSet = null;
        uint texWidth = 0, texHeight = 0;

        // ---- Startup: add overlay render node to the graph ----
        app.AddSystem(Stage.Startup, (World world) =>
        {
            var renderer = world.TryResource<Renderer>();
            if (renderer is not null)
            {
                Logger.Info("Adding OverlayRenderNode to render graph...");
                renderer.AddNode(new OverlayRenderNode());
                Logger.Info("OverlayRenderNode registered (depends on 'sample' node).");
            }
            else
            {
                Logger.Warn("No Renderer resource found — overlay render node not added.");
            }
        });

        // ---- PreUpdate: tick Ultralight DOM/JS ----
        app.AddSystem(Stage.PreUpdate, (World world) =>
        {
            // Lazy-init Ultralight once the window is up
            if (!world.ContainsResource<UltralightContext>())
            {
                var window = world.TryResource<AppWindow>();
                if (window is null) return;

                uint w = (uint)Math.Max(1, window.Sdl.Width);
                uint h = (uint)Math.Max(1, window.Sdl.Height);

                Logger.Info($"Initializing UltralightContext ({w}x{h}) — loading {_url}...");
                var ctx = new UltralightContext(w, h, _url, transparent: true);
                world.InsertResource(ctx);

                // Forward SDL events to Ultralight
                window.SDLEvent += e =>
                {
                    var ulCtx = world.TryResource<UltralightContext>();
                    if (ulCtx?.View is { } view)
                        UltralightInput.ProcessEvent(e, view);
                };

                // Handle resize
                window.ResizeEvent += (newW, newH) =>
                {
                    var ulCtx = world.TryResource<UltralightContext>();
                    ulCtx?.Resize((uint)newW, (uint)newH);
                };

                Logger.Info("UltralightContext created — SDL event and resize handlers wired.");
            }

            world.TryResource<UltralightContext>()?.Update();
        });

        // ---- Render: rasterize the view and upload pixels to the GPU ----
        app.AddSystem(Stage.Render, (World world) =>
        {
            var ulCtx = world.TryResource<UltralightContext>();
            if (ulCtx is null) return;

            var renderer = world.TryResource<Renderer>();
            if (renderer is null || !renderer.Context.IsInitialized) return;

            // Rasterize the Ultralight view to its CPU bitmap
            ulCtx.Render();

            // Read pixels from the bitmap surface
            if (!ulCtx.TryGetPixels(out var pixels, out uint rowBytes))
                return;

            try
            {
                uint viewW = ulCtx.Width;
                uint viewH = ulCtx.Height;
                var gfx = renderer.Context.Graphics;

                // Recreate GPU texture if size changed
                if (gpuImage is null || texWidth != viewW || texHeight != viewH)
                {
                    Logger.Info($"Overlay texture size changed ({texWidth}x{texHeight} → {viewW}x{viewH}) — recreating GPU resources...");
                    gpuImageView?.Dispose();
                    gpuImage?.Dispose();
                    gpuSampler?.Dispose();
                    descriptorSet?.Dispose();

                    texWidth = viewW;
                    texHeight = viewH;

                    var imageDesc = new ImageDesc(
                        new Extent2D(texWidth, texHeight),
                        ImageFormat.B8G8R8A8_UNorm,
                        ImageUsage.Sampled | ImageUsage.TransferDst);

                    gpuImage = gfx.CreateImage(imageDesc);
                    gpuImageView = gfx.CreateImageView(gpuImage);
                    gpuSampler = gfx.CreateSampler(new SamplerDesc(
                        SamplerFilter.Linear, SamplerFilter.Linear,
                        SamplerAddressMode.ClampToEdge, SamplerAddressMode.ClampToEdge,
                        SamplerAddressMode.ClampToEdge));

                    descriptorSet = gfx.CreateDescriptorSet();
                    var samplerBinding = new CombinedImageSamplerBinding(gpuImageView, gpuSampler, 1);
                    gfx.UpdateDescriptorSet(descriptorSet, uniformBinding: null, samplerBinding);

                    Logger.Info($"Overlay GPU texture created: {texWidth}x{texHeight} B8G8R8A8_UNorm (image + view + sampler + descriptor set).");
                }

                // Upload the BGRA pixels to the Vulkan image
                // Ultralight may add padding per row; only copy the tight pixel data
                int bytesPerPixel = 4;
                int expectedStride = (int)texWidth * bytesPerPixel;

                if (rowBytes == (uint)expectedStride)
                {
                    // No padding — upload directly
                    var pixelSlice = pixels.Slice(0, expectedStride * (int)texHeight);
                    gfx.UploadTexture2D(gpuImage, pixelSlice, texWidth, texHeight, bytesPerPixel);
                }
                else
                {
                    // Row padding — copy row-by-row into a tight buffer
                    var tight = new byte[expectedStride * texHeight];
                    for (int y = 0; y < texHeight; y++)
                    {
                        var src = pixels.Slice((int)(y * rowBytes), expectedStride);
                        src.CopyTo(tight.AsSpan(y * expectedStride));
                    }
                    gfx.UploadTexture2D(gpuImage, tight, texWidth, texHeight, bytesPerPixel);
                }

                // Update the sampler binding in case the image was recreated
                var samplerBindingUpdate = new CombinedImageSamplerBinding(gpuImageView!, gpuSampler!, 1);
                gfx.UpdateDescriptorSet(descriptorSet!, uniformBinding: null, samplerBindingUpdate);

                // Store the overlay resource so the OverlayRenderNode can draw it
                var overlayTex = renderer.RenderWorld.TryGet<OverlayTexture>() ?? new OverlayTexture();
                overlayTex.DescriptorSet = descriptorSet;
                renderer.RenderWorld.Set(overlayTex);
            }
            finally
            {
                ulCtx.UnlockPixels();
            }
        });

        // ---- Cleanup: dispose resources ----
        app.AddSystem(Stage.Cleanup, (World world) =>
        {
            Logger.Info("UltralightPlugin: Cleanup — disposing GPU resources and UltralightContext...");
            gpuImageView?.Dispose();
            gpuImage?.Dispose();
            gpuSampler?.Dispose();
            descriptorSet?.Dispose();
            Logger.Debug("Overlay GPU resources disposed (image, view, sampler, descriptor set).");

            if (world.TryResource<UltralightContext>() is { } ctx)
            {
                ctx.Dispose();
                world.RemoveResource<UltralightContext>();
                Logger.Debug("UltralightContext disposed and removed from world.");
            }

            Logger.Info("UltralightPlugin: Cleanup complete.");
        });

        Logger.Info("UltralightPlugin: Build complete — systems registered to Startup, PreUpdate, Render, Cleanup.");
    }
}

