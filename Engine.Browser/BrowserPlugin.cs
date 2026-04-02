namespace Engine;

/// <summary>
/// Plugin that integrates UltralightNet into the engine.
/// Creates a <see cref="BrowserInstance"/> resource, hooks SDL3 events for input,
/// and registers per-frame Update and Cleanup systems.
/// <para>
/// By default loads a simple "about:blank" page. Call
/// <see cref="BrowserInstance.LoadHtml"/> or <see cref="BrowserInstance.LoadUrl"/>
/// after plugin setup to display content.
/// </para>
/// </summary>
public sealed class BrowserPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Browser");

    /// <summary>Optional initial HTML to load. Set before adding to App.</summary>
    public string? InitialHtml { get; init; }

    /// <summary>Optional initial URL to load. Set before adding to App.</summary>
    public string? InitialUrl { get; init; }

    public void Build(App app)
    {
        Logger.Info("BrowserPlugin: Building...");

        var browser = new BrowserInstance();
        app.World.InsertResource(browser);

        // ── Startup: initialize Ultralight and load content ───────────
        app.AddSystem(Stage.Startup, (World world) =>
        {
            var cfg = world.Resource<Config>();
            var b = world.Resource<BrowserInstance>();
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
                // Default: load a minimal test page
                b.LoadHtml(DefaultHtml);
            }

            Logger.Info("BrowserPlugin: Ultralight initialized and content loaded.");

            // Hook SDL events for input forwarding
            if (world.TryResource<AppWindow>() is { } window)
            {
                window.SDLEvent += evt => BrowserInputBridge.ProcessEvent(evt, b);

                // Handle window resize → resize browser view
                window.ResizeEvent += (w, h) =>
                {
                    if (w > 0 && h > 0)
                        b.Resize((uint)w, (uint)h);
                };

                Logger.Info("BrowserPlugin: SDL event hooks registered.");
            }

            // Sync the BrowserInstance into the RenderWorld for the render node
            if (world.TryResource<Renderer>() is { } renderer)
            {
                renderer.RenderWorld.Set(b);
                Logger.Info("BrowserPlugin: BrowserInstance synced to RenderWorld.");
            }
        });

        // ── Per-frame: update Ultralight ──────────────────────────────
        app.AddSystem(Stage.Update, static (World world) =>
        {
            world.TryResource<BrowserInstance>()?.Update();
        });

        // ── Cleanup: dispose Ultralight ──────────────────────────────
        app.AddSystem(Stage.Cleanup, static (World world) =>
        {
            if (world.TryResource<BrowserInstance>() is { } b)
            {
                b.Dispose();
                world.RemoveResource<BrowserInstance>();
            }
        });

        Logger.Info("BrowserPlugin: Build complete.");
    }

    private const string DefaultHtml = """
        <!DOCTYPE html>
        <html>
        <head>
            <title>3DEngine Browser</title>
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body {
                    font-family: 'Segoe UI', Arial, sans-serif;
                    background: transparent;
                    color: #e0e0e0;
                    display: flex;
                    align-items: center;
                    justify-content: center;
                    height: 100vh;
                    overflow: hidden;
                }
                .overlay {
                    background: rgba(20, 20, 30, 0.85);
                    border: 1px solid rgba(100, 130, 255, 0.3);
                    border-radius: 12px;
                    padding: 32px 48px;
                    backdrop-filter: blur(8px);
                    text-align: center;
                    box-shadow: 0 8px 32px rgba(0,0,0,0.5);
                }
                h1 {
                    font-size: 28px;
                    font-weight: 300;
                    margin-bottom: 8px;
                    color: #8090ff;
                }
                p {
                    font-size: 14px;
                    opacity: 0.7;
                }
            </style>
        </head>
        <body>
            <div class="overlay">
                <h1>🌐 3DEngine Browser</h1>
                <p>UltralightNet overlay active</p>
            </div>
        </body>
        </html>
        """;
}


