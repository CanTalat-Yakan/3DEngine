namespace Engine;

/// <summary>Provides core ECS resources (EcsWorld, EcsCommands) and applies command buffer each PostUpdate.</summary>
public sealed class EcsPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.ECS");

    public void Build(App app)
    {
        Logger.Info("EcsPlugin: Registering EcsWorld and EcsCommands resources...");
        app.World.InitResource<EcsWorld>();
        app.World.InitResource<EcsCommands>();

        app.AddSystem(Stage.PostUpdate, (World world) => world.Resource<EcsCommands>().Apply(world.Resource<EcsWorld>()));
        Logger.Info("EcsPlugin: ECS command flush system registered to PostUpdate stage.");
    }
}
