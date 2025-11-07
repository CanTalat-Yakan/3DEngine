namespace Engine;

/// <summary>Provides core ECS resources (EcsWorld, EcsCommands) and applies command buffer each PostUpdate.</summary>
public sealed class EcsPlugin : IPlugin
{
    /// <summary>Inserts ECS resources and adds a PostUpdate system to apply the command buffer.</summary>
    public void Build(App app)
    {
        if (!app.World.ContainsResource<EcsWorld>())
            app.World.InsertResource(new EcsWorld());
        if (!app.World.ContainsResource<EcsCommands>())
            app.World.InsertResource(new EcsCommands());

        // Apply enqueued commands after Update to avoid mutating during iteration.
        app.AddSystem(Stage.PostUpdate, (World world) => world.Resource<EcsCommands>().Apply(world.Resource<EcsWorld>()));
    }
}

/// <summary>
/// Design-time stub for behavior registration; the source generator provides the partial implementation
/// of <see cref="BuildGenerated(App)"/> to register discovered behaviors.
///</summary>
public sealed partial class BehaviorsPlugin : IPlugin
{
    /// <summary>Invokes the source-generated registration method for discovered behaviors.</summary>
    public void Build(App app)
    {
        // Invoke generated registrations if any exist.
        BuildGenerated(app);
    }

    /// <summary>
    /// Implemented by the source generator; registers all behaviors found at compile time.
    /// May be empty if no behaviors exist in the compilation.
    ///</summary>
    static partial void BuildGenerated(App app);
}
