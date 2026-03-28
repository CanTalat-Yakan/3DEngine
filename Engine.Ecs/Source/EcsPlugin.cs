namespace Engine;

/// <summary>Provides core ECS resources (EcsWorld, EcsCommands) and applies command buffer each PostUpdate.</summary>
public sealed class EcsPlugin : IPlugin
{
    public void Build(App app)
    {
        if (!app.World.ContainsResource<EcsWorld>())
            app.World.InsertResource(new EcsWorld());
        if (!app.World.ContainsResource<EcsCommands>())
            app.World.InsertResource(new EcsCommands());

        app.AddSystem(Stage.PostUpdate, (World world) => world.Resource<EcsCommands>().Apply(world.Resource<EcsWorld>()));
    }
}
