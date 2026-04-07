namespace Engine;

/// <summary>
/// Plugin contract for extending the application.
/// Plugins insert resources, register systems, and compose other plugins during
/// the one-time <see cref="Build"/> phase before the main loop starts.
/// </summary>
/// <remarks>
/// Plugins are the primary composition unit for the engine. Each plugin encapsulates a
/// self-contained feature (e.g., time tracking, input, rendering) and is added to the
/// <see cref="App"/> via <see cref="App.AddPlugin"/>. The <see cref="Build"/> method is
/// called exactly once during application setup, before any systems execute.
/// </remarks>
/// <example>
/// <code>
/// public class PhysicsPlugin : IPlugin
/// {
///     public void Build(App app)
///     {
///         app.World.InitResource&lt;PhysicsWorld&gt;();
///         app.AddSystem(Stage.PreUpdate, new SystemDescriptor(PhysicsSystem.Step, "Physics.Step")
///             .Write&lt;PhysicsWorld&gt;());
///     }
/// }
/// </code>
/// </example>
/// <seealso cref="App"/>
/// <seealso cref="App.AddPlugin"/>
public interface IPlugin
{
    /// <summary>Called once during app setup to configure resources, systems, and sub-plugins.</summary>
    /// <param name="app">The application builder to register resources and systems with.</param>
    void Build(App app);
}

