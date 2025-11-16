namespace Engine;

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