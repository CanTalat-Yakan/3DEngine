namespace Engine;

public sealed class Program
{
    public AppWindow AppWindow { get; private set; }
    public Kernel Kernel { get; private set; }

    [STAThread]
    private static void Main() =>
        new Program().Run(sceneBoot: true);

    public void Run(bool renderGUI = true, bool sceneBoot = false, Config config = null, Delegate initialization = null, Delegate frame = null)
    {
        HandleExceptions();

        Initialize(renderGUI, sceneBoot, config);

        initialization?.DynamicInvoke();

        AppWindow.Looping(Kernel.Frame, frame);
        AppWindow.Dispose(Kernel.Dispose);
    }

    private void Initialize(bool renderGUI, bool sceneBoot, Config config)
    {
        if (config is null)
        {
            config = Config.GetDefault();
            config.SetResolutionScale(1);
            config.SetMSAA(MultiSample.x4);
        }
        config.GUI = renderGUI;
        config.SceneBoot = sceneBoot;

        AppWindow = new(config.WindowData);
        AppWindow.Show(config.WindowCommand);

        Kernel = new(config);
        Kernel.Initialize(AppWindow.Win32Window.Handle, AppWindow.Win32Window.Size, win32Window: true);

        AppWindow.ResizeEvent += Kernel.Context.GraphicsDevice.Resize;
    }

    private static void HandleExceptions()
    {
        var rootPath = AppContext.BaseDirectory;
        var logFilePath = rootPath + "Application.log";

        ExceptionHandler.CreateTraceLog(rootPath, logFilePath);

        AppDomain.CurrentDomain.UnhandledException += (s, e) =>
        {
            if (e.ExceptionObject is Exception exception)
                ExceptionHandler.HandleException(exception);
        };
    }
}