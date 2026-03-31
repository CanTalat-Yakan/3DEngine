namespace Engine;

/// <summary>
/// Plugin contract for extending the application.
/// Plugins insert resources, register systems, and compose other plugins during
/// the one-time <see cref="Build"/> phase before the main loop starts.
/// </summary>
public interface IPlugin
{
    /// <summary>Called once during app setup to configure the app/world.</summary>
    void Build(App app);
}

