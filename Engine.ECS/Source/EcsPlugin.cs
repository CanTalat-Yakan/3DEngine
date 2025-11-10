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
