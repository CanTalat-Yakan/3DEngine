namespace Engine;

public sealed class DefaultPlugins : IPlugin
{
    public void Build(App app)
    {
        app.AddPlugin(new WindowPlugin())
           .AddPlugin(new ExceptionsPlugin())
           .AddPlugin(new AppExitPlugin())
           .AddPlugin(new TimePlugin())
           .AddPlugin(new InputPlugin())
           .AddPlugin(new EventsPlugin())
           .AddPlugin(new ECSPlugin())
           .AddPlugin(new GeneratedBehaviorsPlugin())
           .AddPlugin(new KernelPlugin())
           .AddPlugin(new GUIPlugin())
           .AddPlugin(new ClearColorPlugin());

        // Clear per-frame changed flags in EcsWorld at stage First
        app.AddSystem(Stage.First, (World w) => w.Resource<ECSWorld>().BeginFrame());
    }
}
