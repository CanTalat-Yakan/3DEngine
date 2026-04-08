namespace Engine;

/// <summary>
/// Plugin that integrates UltralightNet into the engine.
/// Creates a <see cref="WebViewInstance"/> resource, hooks SDL3 events for input,
/// and registers per-frame Update and Cleanup systems.
/// <para>
/// By default loads a simple "about:blank" page. Call
/// <see cref="WebViewInstance.LoadHtml"/> or <see cref="WebViewInstance.LoadUrl"/>
/// after plugin setup to display content.
/// </para>
/// </summary>
public sealed class WebViewPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView");

    /// <summary>Delay (ms) after the last resize event before committing the native
    /// <see cref="WebViewInstance.Resize"/> call (which triggers a surface reallocation).</summary>
    private const long ResizeDebounceMs = 150;

    /// <summary>Optional initial HTML to load. Set before adding to App.</summary>
    public string? InitialHtml { get; init; }

    /// <summary>Optional initial URL to load. Set before adding to App.</summary>
    public string? InitialUrl { get; init; }

    /// <summary>
    /// Rendering mode for the webview. Defaults to <see cref="WebViewMode.Gpu"/> for GPU-accelerated
    /// rendering via the Vulkan GPU driver. Set to <see cref="WebViewMode.Cpu"/> to fall back to
    /// CPU bitmap surface rendering.
    /// </summary>
    public WebViewMode Mode { get; init; } = WebViewMode.Gpu;

    /// <inheritdoc />
    public void Build(App app)
    {
        Logger.Info($"WebViewPlugin: Building (mode={Mode})...");

        var webview = new WebViewInstance();
        app.World.InsertResource(webview);

        // Capture the mode for closures
        var mode = Mode;

        // ── Debounce state for WebView native resize ──
        bool pendingWebViewResize = false;
        uint pendingW = 0, pendingH = 0;
        long lastWebViewResizeTick = 0;

        // ── Startup: initialize Ultralight and load content ───────────
        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
            {
                var cfg = world.Resource<Config>();
                var b = world.Resource<WebViewInstance>();

                // Create GPU driver if GPU mode is selected and Vulkan is active
                VulkanUltralightGpuDriver? gpuDriver = null;
                if (mode == WebViewMode.Gpu && cfg.Graphics == GraphicsBackend.Vulkan)
                {
                    if (world.TryGetResource<Renderer>(out var renderer) && renderer.Context.IsInitialized)
                    {
                        gpuDriver = new VulkanUltralightGpuDriver(renderer.Context.Graphics);
                        world.InsertResource(gpuDriver);
                        Logger.Info("WebViewPlugin: GPU driver created and registered as resource.");
                    }
                    else
                    {
                        Logger.Warn("WebViewPlugin: GPU mode requested but Vulkan renderer not initialized. Falling back to CPU mode.");
                        mode = WebViewMode.Cpu;
                    }
                }
                else if (mode == WebViewMode.Gpu)
                {
                    Logger.Warn($"WebViewPlugin: GPU mode requires Vulkan backend (current: {cfg.Graphics}). Falling back to CPU mode.");
                    mode = WebViewMode.Cpu;
                }

                // Initialize at config dimensions
                b.Initialize((uint)cfg.WindowData.Width, (uint)cfg.WindowData.Height, mode, gpuDriver);

                if (InitialHtml is not null)
                {
                    b.LoadHtml(InitialHtml);
                }
                else if (InitialUrl is not null)
                {
                    b.LoadUrl(InitialUrl);
                }
                else
                {
                    // Default: load the embedded test page
                    b.LoadHtml(LoadDefaultHtml());
                }

                Logger.Info($"WebViewPlugin: Ultralight initialized (mode={mode}) and content loaded.");

                // Hook SDL events for input forwarding
                if (world.TryGetResource<AppWindow>(out var window))
                {
                    window.SDLEvent += evt => WebViewInput.ProcessEvent(evt, b);

                    // Handle window resize → flag for debounced native resize
                    window.ResizeEvent += (w, h) =>
                    {
                        if (w > 0 && h > 0)
                        {
                            pendingWebViewResize = true;
                            pendingW = (uint)w;
                            pendingH = (uint)h;
                            lastWebViewResizeTick = Environment.TickCount64;
                        }
                    };

                    Logger.Info("WebViewPlugin: SDL event hooks registered.");
                }

                // Sync the WebViewInstance into the RenderWorld for the render node
                if (world.TryGetResource<Renderer>(out var rend))
                {
                    rend.RenderWorld.Set(b);
                    // Also sync the GPU driver to the render world if in GPU mode
                    if (gpuDriver is not null)
                        rend.RenderWorld.Set(gpuDriver);
                    Logger.Info("WebViewPlugin: WebViewInstance synced to RenderWorld.");
                }
            }, "WebViewPlugin.Startup")
            .MainThreadOnly()
            .Read<Config>()
            .Write<WebViewInstance>()
            .Write<AppWindow>()
            .Write<Renderer>());

        // ── Per-frame: resolve debounced resize, then update Ultralight ──
        app.AddSystem(Stage.Update, new SystemDescriptor((world) =>
            {
                var b = world.TryResource<WebViewInstance>();
                if (b is null) return;
            
                if (pendingWebViewResize && (Environment.TickCount64 - lastWebViewResizeTick) >= ResizeDebounceMs)
                {
                    pendingWebViewResize = false;
                    Logger.Debug($"WebView debounce elapsed - committing native resize to {pendingW}x{pendingH}.");
                    b.Resize(pendingW, pendingH);
                }
            
                b.Update();
            }, name: "WebViewPlugin.Update")
            .MainThreadOnly()
            .Write<WebViewInstance>()
            .Read<AppWindow>());

        // ── Cleanup: dispose Ultralight ──────────────────────────────
        app.AddSystem(Stage.Cleanup, new SystemDescriptor(static (World world) =>
            {
                if (world.TryGetResource<VulkanUltralightGpuDriver>(out var gpuDriver))
                {
                    gpuDriver.Dispose();
                    world.RemoveResource<VulkanUltralightGpuDriver>();
                }
                if (world.TryGetResource<WebViewInstance>(out var b))
                {
                    b.Dispose();
                    world.RemoveResource<WebViewInstance>();
                }
            }, "WebViewPlugin.Cleanup")
            .MainThreadOnly()
            .Write<WebViewInstance>());

        Logger.Info("WebViewPlugin: Build complete.");
    }

    private static string LoadDefaultHtml()
    {
        using var stream = typeof(WebViewPlugin).Assembly
            .GetManifestResourceStream("Engine.WebView.default.html");
        if (stream is null)
            throw new InvalidOperationException("Embedded resource 'Engine.WebView.default.html' not found.");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}


