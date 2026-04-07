namespace Engine;

/// <summary>Provides core ECS resources (<see cref="EcsWorld"/>, <see cref="EcsCommands"/>) and flushes the command buffer each <see cref="Stage.PostUpdate"/>.</summary>
/// <remarks>
/// This plugin initialises the <see cref="EcsWorld"/> and <see cref="EcsCommands"/> resources and
/// registers a <see cref="Stage.PostUpdate"/> system that calls <see cref="EcsCommands.Apply"/>
/// to flush deferred spawn/despawn/add/remove operations in FIFO order.
/// </remarks>
/// <seealso cref="EcsWorld"/>
/// <seealso cref="EcsCommands"/>
/// <seealso cref="BehaviorsPlugin"/>
public sealed class EcsPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.ECS");

    /// <inheritdoc />
    public void Build(App app)
    {
        Logger.Info("EcsPlugin: Registering EcsWorld and EcsCommands resources...");
        app.World.InitResource<EcsWorld>();
        app.World.InitResource<EcsCommands>();

        app.AddSystem(Stage.PostUpdate, new SystemDescriptor(world =>
            {
                world.Resource<EcsCommands>().Apply(world.Resource<EcsWorld>());
            }, "EcsPlugin.FlushCommands")
            .Write<EcsCommands>()
            .Write<EcsWorld>());
        Logger.Info("EcsPlugin: ECS command flush system registered to PostUpdate stage.");
    }
}
