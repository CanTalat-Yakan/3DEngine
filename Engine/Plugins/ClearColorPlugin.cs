namespace Engine;

/// <summary>Ensures a default RenderClearColor resource exists.</summary>
public sealed class ClearColorPlugin : IPlugin
{
    /// <summary>Inserts a default clear color resource if missing.</summary>
    public void Build(App app)
    {
        if (!app.World.ContainsResource<RenderClearColor>())
        {
            var cfg = app.World.Resource<Config>();
            var color = cfg.Graphics == GraphicsBackend.Vulkan
                ? new RenderClearColor(0.675f, 0.086f, 0.173f, 1f)   // Tamarillo red for Vulkan
                : new RenderClearColor(0.45f, 0.55f, 0.60f, 1.00f);  // blue-ish for SDL
            app.World.InsertResource(color);
        }
    }
}
