namespace Engine;

public sealed class WindowPlugin : IPlugin
{
    public void Build(App app)
    {
        var config = app.World.Resource<Config>();
        var window = new AppWindow(config.WindowData);
        window.Show(config.WindowCommand);
        app.World.InsertResource(window);
    }
}

