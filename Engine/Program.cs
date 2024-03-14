namespace Engine;

public sealed class Program
{
    public AppWindow AppWindow { get; private set; }
    public Kernel Kernel { get; private set; }

    [STAThread]
    private static void Main() =>
        new Program().Run();

    public void Run(bool renderGUI = true, Config config = null)
    {
        HandleExceptions();

        Initialize(renderGUI, config);

        AppWindow.Looping(Kernel.Frame);
        AppWindow.Dispose(Kernel.Dispose);
    }

    private void Initialize(bool renderGUI, Config config)
    {
        config ??= Config.GetDefault();
        config.SetResolutionScale(2);
        config.GUI = renderGUI;

        AppWindow = new(config.WindowData);
        AppWindow.Show(Interoperation.ShowWindowCommand.Maximize);

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
            var exception = e.ExceptionObject as Exception;
            if (exception is not null)
                ExceptionHandler.HandleException(exception);
        };
    }
}