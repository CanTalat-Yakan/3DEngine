namespace Engine;

/// <summary>Ensures a default ClearColor resource exists.</summary>
public sealed class ClearColorPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.ClearColor");

    /// <summary>Inserts a default clear color resource if missing.</summary>
    public void Build(App app)
    {
        var cfg = app.World.Resource<Config>();
        var color = app.World.GetOrInsertResource(() => cfg.Graphics == GraphicsBackend.Vulkan
            ? new ClearColor(0.675f, 0.086f, 0.173f, 1f)   // Tamarillo red for Vulkan
            : new ClearColor(0.45f, 0.55f, 0.60f, 1.00f)); // blue-ish for SDL
        Logger.Info($"ClearColorPlugin: Clear color (R={color.R:F2}, G={color.G:F2}, B={color.B:F2}, A={color.A:F2}) for {cfg.Graphics} backend.");
    }
}
