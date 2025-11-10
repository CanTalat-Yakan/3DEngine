namespace Engine;

/// <summary>Aggregates the standard set of engine plugins and installs a frame-begin ECS housekeeping system.</summary>
public sealed class DefaultPlugins : IPlugin
{
    /// <summary>Builds and composes the default engine plugin stack and adds a First-stage system that resets per-frame change tracking in the <see cref="EcsWorld"/>.</summary>
    public void Build(App app)
    {
        // Compose common plugins in conventional order (window -> diagnostics -> lifecycle -> frame services -> UI)
        app.AddPlugin(new AppWindowPlugin())
           .AddPlugin(new AppExitPlugin())
           .AddPlugin(new ExceptionsPlugin())
           .AddPlugin(new TimePlugin())
           .AddPlugin(new InputPlugin())
           .AddPlugin(new EcsPlugin())
           .AddPlugin(new BehaviorsPlugin())
           .AddPlugin(new ImGuiPlugin())
           .AddPlugin(new ClearColorPlugin())
           .AddPlugin(new VulkanRendererPlugin());

        // Clear per-frame changed flags in EcsWorld at stage First (pre update logic)
        app.AddSystem(Stage.First, (World world) => world.Resource<EcsWorld>().BeginFrame());
    }
}
