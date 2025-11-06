namespace Engine;

public sealed class DefaultPlugins : IPlugin
{
    public void Build(App app)
    {
        app.AddPlugin(new ExceptionsPlugin())
           .AddPlugin(new WindowPlugin())
           .AddPlugin(new TimePlugin())
           .AddPlugin(new EventsPlugin())
           .AddPlugin(new InputPlugin())
           .AddPlugin(new AppExitPlugin())
           .AddPlugin(new ECSPlugin())
           .AddPlugin(new Generated.GeneratedBehavioursPlugin())
           .AddPlugin(new KernelPlugin())
           .AddPlugin(new ImGuiPlugin())
           .AddPlugin(new ClearColorPlugin());

        // Clear per-frame changed flags in EcsWorld at stage First
        app.AddSystem(Stage.First, (World w) => w.Resource<ECSWorld>().BeginFrame());
    }
}
