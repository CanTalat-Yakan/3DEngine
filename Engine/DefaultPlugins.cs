namespace Engine;

/// <summary>Aggregates the standard set of engine plugins.</summary>
public sealed class DefaultPlugins : IPlugin
{
    public void Build(App app)
    {
        app.AddPlugin(new AppWindowPlugin())
            .AddPlugin(new AppExitPlugin())
            .AddPlugin(new ExceptionsPlugin())
            .AddPlugin(new TimePlugin())
            .AddPlugin(new InputPlugin())
            .AddPlugin(new EcsPlugin())
            .AddPlugin(new BehaviorsPlugin())
            .AddPlugin(new SdlImGuiPlugin())
            .AddPlugin(new ClearColorPlugin())
            .AddPlugin(new SdlRendererPlugin());

        app.AddSystem(Stage.First, (World world) => world.Resource<EcsWorld>().BeginFrame());
    }
}
