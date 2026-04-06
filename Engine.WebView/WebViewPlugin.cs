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

    /// <summary>Optional initial HTML to load. Set before adding to App.</summary>
    public string? InitialHtml { get; init; }

    /// <summary>Optional initial URL to load. Set before adding to App.</summary>
    public string? InitialUrl { get; init; }

    public void Build(App app)
    {
        Logger.Info("WebViewPlugin: Building...");

        var webview = new WebViewInstance();
        app.World.InsertResource(webview);

        // ── Startup: initialize Ultralight and load content ───────────
        app.AddSystem(Stage.Startup, (World world) =>
        {
            var cfg = world.Resource<Config>();
            var b = world.Resource<WebViewInstance>();

            // Initialize at config dimensions — if the window is actually
            // larger (e.g. HiDPI scaling), the deferred resize in Update()
            // will handle it safely without crashing native code.
            b.Initialize((uint)cfg.WindowData.Width, (uint)cfg.WindowData.Height);

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

            Logger.Info("WebViewPlugin: Ultralight initialized and content loaded.");

            // Hook SDL events for input forwarding
            if (world.TryGetResource<AppWindow>(out var window))
            {
                window.SDLEvent += evt => WebViewInput.ProcessEvent(evt, b);

                // Handle window resize → resize webview view
                window.ResizeEvent += (w, h) =>
                {
                    if (w > 0 && h > 0)
                        b.Resize((uint)w, (uint)h);
                };

                Logger.Info("WebViewPlugin: SDL event hooks registered.");
            }

            // Sync the WebViewInstance into the RenderWorld for the render node
            if (world.TryGetResource<Renderer>(out var renderer))
            {
                renderer.RenderWorld.Set(b);
                Logger.Info("WebViewPlugin: WebViewInstance synced to RenderWorld.");
            }
        });

        // ── Per-frame: update Ultralight ──────────────────────────────
        app.AddSystem(Stage.Update, static (World world) =>
        {
            world.TryResource<WebViewInstance>()?.Update();
        });

        // ── Cleanup: dispose Ultralight ──────────────────────────────
        app.AddSystem(Stage.Cleanup, static (World world) =>
        {
            if (world.TryGetResource<WebViewInstance>(out var b))
            {
                b.Dispose();
                world.RemoveResource<WebViewInstance>();
            }
        });

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


