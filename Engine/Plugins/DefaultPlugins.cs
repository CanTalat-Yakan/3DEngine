namespace Engine;

/// <summary>Aggregates the standard set of engine plugins.</summary>
public sealed class DefaultPlugins : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Plugins");

    public void Build(App app)
    {
        Logger.Info("DefaultPlugins: Loading standard engine plugin set...");

        app.AddPlugin(new AppWindowPlugin())
            .AddPlugin(new AppExitPlugin())
            .AddPlugin(new ExceptionsPlugin())
            .AddPlugin(new TimePlugin())
            .AddPlugin(new InputPlugin())
            .AddPlugin(new EcsPlugin())
            .AddPlugin(new BehaviorsPlugin())
            .AddPlugin(new SdlImGuiPlugin())
            .AddPlugin(new ClearColorPlugin())
            .AddPlugin(new SdlRendererPlugin())
            .AddPlugin(new VulkanImGuiPlugin());

        app.AddSystem(Stage.First, (World world) => world.Resource<EcsWorld>().BeginFrame());

        Logger.Info("DefaultPlugins: All standard plugins loaded successfully.");
    }
}
