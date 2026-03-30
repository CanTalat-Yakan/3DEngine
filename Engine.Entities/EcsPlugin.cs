namespace Engine;

/// <summary>Provides core ECS resources (EcsWorld, EcsCommands) and applies command buffer each PostUpdate.</summary>
public sealed class EcsPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.ECS");

    public void Build(App app)
    {
        Logger.Info("EcsPlugin: Registering EcsWorld and EcsCommands resources...");
        if (!app.World.ContainsResource<EcsWorld>())
            app.World.InsertResource(new EcsWorld());
        if (!app.World.ContainsResource<EcsCommands>())
            app.World.InsertResource(new EcsCommands());

        app.AddSystem(Stage.PostUpdate, (World world) => world.Resource<EcsCommands>().Apply(world.Resource<EcsWorld>()));
        Logger.Info("EcsPlugin: ECS command flush system registered to PostUpdate stage.");
    }
}
