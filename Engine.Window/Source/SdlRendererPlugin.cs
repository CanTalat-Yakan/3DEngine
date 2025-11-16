namespace Engine;

/// <summary>Plugin that initializes the Vulkan renderer and drives it each Render stage.</summary>
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
        // Only initialize Vulkan if the window was created with Vulkan flag
        if (cfg.Graphics == GraphicsBackend.Vulkan)
        {
            var surface = new SdlSurfaceSource(window.Sdl);
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
            // Only render when the graphics device has been initialized (e.g., Vulkan selected and initialized).
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
