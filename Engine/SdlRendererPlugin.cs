namespace Engine;

/// <summary>Composition-root plugin that wires the Vulkan renderer to the platform window.</summary>
public sealed class SdlRendererPlugin : IPlugin
{
    private sealed class ClearColorExtract : IExtractSystem
    {
        public void Run(object appWorld, RenderWorld renderWorld)
        {
            if (appWorld is World w && w.TryResource<RenderClearColor>() is { } cc)
            {
                renderWorld.Set(cc);
            }
        }
    }

    public void Build(App app)
    {
        var renderer = new Renderer();
        renderer.AddExtractSystem(new ClearColorExtract());
        renderer.AddExtractSystem(new CameraExtract());
        renderer.AddExtractSystem(new MeshMaterialExtract());
        renderer.AddPrepareSystem(new SamplePrepare());
        renderer.AddQueueSystem(new SampleQueue());
        app.World.InsertResource(renderer);

        // Initialize Vulkan against SDL window if configured
        var cfg = app.World.Resource<Config>();
        var window = app.World.Resource<AppWindow>();

        if (cfg.Graphics == GraphicsBackend.Vulkan)
        {
            // Grab the ISurfaceSource that AppWindowPlugin inserted
            var surface = app.World.Resource<ISurfaceSource>();
            renderer.Context.Initialize(surface, cfg.WindowData.Title);

            // Seed RenderSurfaceInfo with current window size
            var surfaceInfo = new RenderSurfaceInfo { Width = window.Sdl.Width, Height = window.Sdl.Height };
            renderer.RenderWorld.Set(surfaceInfo);

            // Handle resize -> update surface info and recreate swapchain
            window.ResizeEvent += (w, h) =>
            {
                if (w > 0 && h > 0)
                {
                    renderer.RenderWorld.Set(new RenderSurfaceInfo { Width = w, Height = h });
                    renderer.Context.OnResize();
                }
            };
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
                r.Dispose();
                world.RemoveResource<Renderer>();
            }
        });
    }
}

/// <summary>Ensures a default RenderClearColor resource exists.</summary>
public sealed class ClearColorPlugin : IPlugin
{
    /// <summary>Inserts a default clear color resource if missing.</summary>
    public void Build(App app)
    {
        if (!app.World.ContainsResource<RenderClearColor>())
            app.World.InsertResource(new RenderClearColor(0.45f, 0.55f, 0.60f, 1.00f));
    }
}
