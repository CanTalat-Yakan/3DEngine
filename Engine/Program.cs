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
        InitializeWindow(renderGUI, ref config, out var appWindow);
        InitializeEngine(appWindow, ref config, out var engineCore);

        // Create a while loop for the game logic
        // and dispose on window quit requested.
        appWindow.Loop(
            engineCore.Frame);

        engineCore.Dispose();
    }

    private void InitializeWindow(bool renderGUI, ref Config config, out AppWindow appWindow)
    {
        config ??= Config.GetDefault();
        config.GUI = renderGUI;

        appWindow = new();
        appWindow.Initialize(config.WindowData);
        appWindow.Show();
    }

    private void InitializeEngine(AppWindow appWindow, ref Config config, out Core engineCore)
    {
        engineCore = new Core(new Renderer(appWindow.Win32Window, config), appWindow.Win32Window.Handle);
        engineCore.OnGUI += appWindow.Render;

        appWindow.ResizeEvent += engineCore.Renderer.Resize;
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