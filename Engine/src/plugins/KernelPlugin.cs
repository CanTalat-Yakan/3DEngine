namespace Engine;

public sealed class KernelPlugin : IPlugin
{
    public void Build(App app)
    {
        var cfg = app.World.Resource<Config>();
        var window = app.World.Resource<AppWindow>().SdlWindow;

        var kernel = new Kernel(cfg);
        kernel.Initialize(window.Window, (window.Width, window.Height));
        app.World.InsertResource(kernel);

        app.AddSystem(Stage.First, (World w) => w.Resource<Kernel>().Frame());
    }
}

public sealed class Kernel
{
    public Kernel(Config config)
    {
    }

    public void Initialize(IntPtr nativeWindow, (int Width, int Height) windowSize)
    {
    }

    public void Frame()
    {
    }

    public void Dispose()
    {
    }
}
