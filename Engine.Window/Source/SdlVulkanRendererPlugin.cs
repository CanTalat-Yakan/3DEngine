namespace Engine;

/// <summary>Plugin that initializes the Vulkan renderer and drives it each Render stage.</summary>
public sealed class SdlVulkanRendererPlugin : IPlugin
{
    private sealed class ClearColorExtract : IExtractSystem
    {
        public void Run(object appWorld, RenderWorld renderWorld)
        {
            if (appWorld is World w && w.TryResource<ClearColor>() is { } cc)
            {
                var v = cc.Value;
                renderWorld.Set(new RenderClearColor(v.X, v.Y, v.Z, v.W));
            }
        }
    }

    public void Build(App app)
    {
        var renderer = new Renderer();
        renderer.AddExtractSystem(new ClearColorExtract());
        renderer.AddExtractSystem(new CameraExtract());
        renderer.AddExtractSystem(new MeshMaterialExtract());
        renderer.AddPrepareSystem(new PreparePlaceholder());
        renderer.AddQueueSystem(new QueuePlaceholder());
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
            renderer.RenderFrame(world);
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
