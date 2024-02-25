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

        // Instantiate Window and Engine.
        Initialize(renderGUI, ref config);

        // Create a while loop for the game logic
        // and dispose on window quit request.
        AppWindow.Looping(Kernel.Frame);
        AppWindow.Dispose(Kernel.Dispose);
    }

    private void Initialize(bool renderGUI, ref Config config)
    {
        config ??= Config.GetDefault();
        config.GUI = renderGUI;

        AppWindow = new(config.WindowData);
        AppWindow.Show();

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
            // This method will be called when an unhandled exception occurs.
            var exception = e.ExceptionObject as Exception;
            if (exception is not null)
                ExceptionHandler.HandleException(exception);
        };
    }
}