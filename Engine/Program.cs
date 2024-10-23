namespace Engine;

public sealed class Program
{
    public AppWindow AppWindow { get; private set; }
    public Kernel Kernel { get; private set; }

    [STAThread]
    private static void Main() =>
        new Program().Run();

    public void Run(Config config = null, Delegate initialization = null, Delegate frame = null)
    {
        HandleExceptions();

        Initialize(config);

        initialization?.DynamicInvoke();

        AppWindow.Looping(Kernel.Frame, frame);
        AppWindow.Dispose(Kernel.Dispose);
    }

    private void Initialize(Config config)
    {
        config ??= Config.GetDefault(multiSample: MultiSample.x4, defaultBoot: true);

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