namespace Engine;

public sealed class Program
{
    [STAThread]
    private static void Main() =>
        new Program().Run(false);

    public void Run(bool renderGUI = true, Config config = null)
    {
        HandleExceptions();

        // Instantiate Window and Engine.
        Initialize(renderGUI, ref config, 
            out var appWindow, 
            out var engineCore);

        // Create a while loop for the game logic
        // and dispose on window quit request.
        appWindow.Looping(engineCore.Frame);
        appWindow.Dispose(engineCore.Dispose);
    }

    private void Initialize(bool renderGUI, ref Config config, out AppWindow appWindow, out Core engineCore)
    {
        config ??= Config.GetDefault();
        config.GUI = renderGUI;

        appWindow = new(config.WindowData);
        appWindow.Show();

        engineCore = new Core(new Renderer(AppWindow.Win32Window, config), AppWindow.Win32Window.Handle);
        engineCore.OnGUI += appWindow.Render;

        AppWindow.ResizeEvent += engineCore.Renderer.Resize;
    }

    private static void HandleExceptions()
    {
        var rootPath = Paths.DIRECTORY;
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