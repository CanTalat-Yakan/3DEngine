namespace Engine;

public sealed class Program
{
    [STAThread]
    private static void Main() =>
        new Program().Run(false);

    public void Run(bool gui = true, Config config = null)
    {
        HandleExceptions();

        // Instantiate AppWindow and Engine, then show Window.
        Initialize(gui, config, 
            out var appWindow, 
            out var engineCore);

        // Create a while loop for the game logic
        // and break when the window requested to quit.
        while (appWindow.IsAvailable())
            engineCore?.Frame();

        engineCore?.Dispose();
    }

    private void Initialize(bool gui, Config config, out AppWindow appWindow, out Core engineCore)
    {
        config ??= Config.GetDefaultConfig();
        config.SetWindowData("3D Engine", 1080, 720);
        config.GUI = gui;

        appWindow = new();
        appWindow.Initialize(config.WindowData);

        engineCore = null;
        //engineCore = new Core(new Renderer(appWindow.Win32Window, config), appWindow.Win32Window.Handle);
        //engineCore.OnGUI += appWindow.Render;

        //appWindow.ResizeEvent += Core.Instance.Renderer.Resize;

        appWindow.Show();
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