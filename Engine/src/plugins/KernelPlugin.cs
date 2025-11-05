namespace Engine;

public sealed class KernelPlugin : IPlugin
{
    public void Build(App app)
    {
        var cfg = app.World.Resource<Config>();
        var window = app.World.Resource<AppWindow>().SdlWindow;

        var kernel = new Kernel(cfg);
        kernel.Initialize(window.Window, (window.Width, window.Height), win32Window: false);
        app.World.InsertResource(kernel);

        app.AddSystem(Stage.First, (World w) => w.Resource<Kernel>().Frame());
    }
}

