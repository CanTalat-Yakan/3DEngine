namespace Engine;

/// <summary>Composition-root plugin that wires the Vulkan renderer to the platform window.</summary>
public sealed class SdlRendererPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.SdlRenderer");

    private sealed class ClearColorExtract : IExtractSystem
    {
        public void Run(object appWorld, RenderWorld renderWorld)
        {
            if (appWorld is World w && w.TryResource<ClearColor>() is { } cc)
            {
                renderWorld.Set(cc);
            }
        }
    }

    public void Build(App app)
    {
        Logger.Info("SdlRendererPlugin: Creating Renderer and wiring extract/prepare/queue systems...");
        var renderer = new Renderer();
        renderer.AddExtractSystem(new ClearColorExtract());
        renderer.AddExtractSystem(new CameraExtract());
        renderer.AddExtractSystem(new MeshMaterialExtract());
        renderer.AddPrepareSystem(new SamplePrepare());
        renderer.AddQueueSystem(new SampleQueue());
        app.World.InsertResource(renderer);
        Logger.Debug("Renderer resource registered with extract, prepare, and queue systems.");

        // Initialize Vulkan against SDL window if configured
        var cfg = app.World.Resource<Config>();
        var window = app.World.Resource<AppWindow>();

        if (cfg.Graphics == GraphicsBackend.Vulkan)
        {
            Logger.Info("SdlRendererPlugin: Vulkan backend selected — initializing graphics context against SDL window...");
            // Grab the ISurfaceSource that AppWindowPlugin inserted
            var surface = app.World.Resource<ISurfaceSource>();
            renderer.Context.Initialize(surface, cfg.WindowData.Title);

            // Seed RenderSurfaceInfo with current window size
            var surfaceInfo = new RenderSurfaceInfo { Width = window.Sdl.Width, Height = window.Sdl.Height };
            renderer.RenderWorld.Set(surfaceInfo);
            Logger.Info($"Initial render surface: {surfaceInfo.Width}x{surfaceInfo.Height}");

            // Handle resize -> update surface info and recreate swapchain
            window.ResizeEvent += (w, h) =>
            {
                if (w > 0 && h > 0)
                {
                    Logger.Info($"Window resized to {w}x{h} — updating render surface and swapchain...");
                    renderer.RenderWorld.Set(new RenderSurfaceInfo { Width = w, Height = h });
                    renderer.Context.OnResize();
                }
            };
        }
        else
        {
            Logger.Info("SdlRendererPlugin: Non-Vulkan backend — Vulkan renderer initialization skipped.");
        }

        // Run Vulkan renderer after other Render stage systems
        app.AddSystem(Stage.Render, (world) =>
        {
            if (world.TryResource<Renderer>() is { } r && r.Context.IsInitialized)
            {
                r.RenderFrame(world);
            }
        });

        // Ensure disposal at app exit (Cleanup stage)
        app.AddSystem(Stage.Cleanup, (world) =>
        {
            if (world.TryResource<Renderer>() is { } r)
            {
                Logger.Info("SdlRendererPlugin: Cleanup stage — disposing Renderer...");
                r.Dispose();
                world.RemoveResource<Renderer>();
            }
        });

        Logger.Info("SdlRendererPlugin: Build complete.");
    }
}
