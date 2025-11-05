namespace Engine;

public sealed class DefaultPlugins : IPlugin
{
    public void Build(App app)
    {
        app.AddPlugin(new ExceptionHandlingPlugin())
           .AddPlugin(new WindowPlugin())
           .AddPlugin(new TimePlugin())
           .AddPlugin(new EventsPlugin())
           .AddPlugin(new InputPlugin())
           .AddPlugin(new AppExitPlugin())
           .AddPlugin(new KernelPlugin())
           .AddPlugin(new ImGuiPlugin())
           .AddPlugin(new ClearColorPlugin());
    }
}
