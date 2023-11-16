using Vortice.Win32;

namespace Engine;

using static Vortice.Win32.User32;

public sealed class Program
{
    [STAThread]
    private static void Main() =>
        new Program().Run(false);

    private void Loop() =>
        Core.Instance.Frame();

    public void Run(bool renderGui = true, Config config = null)
    {
        HandleExceptions();

        // Instantiate AppWindow and Engine, then show Window.
        Initialize(renderGui, config);

        // Create a while loop and break when the window requested to quit.
        while (true)
        {
            if (PeekMessage(out var msg, IntPtr.Zero, 0, 0, 1))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);

                if (msg.Value == (uint)WindowMessage.Quit)
                {
                    Core.Instance.Dispose();

                    break;
                }
            }

            Loop(); // <-- This is where the loop is handled.
        }
    }

    private void Initialize(bool renderGui, Config config)
    {
        AppWindow appWindow = new();

        appWindow.CreateWindow(out var wndClass);
        appWindow.Initialize(new Win32Window(wndClass.ClassName,
            "3D Engine",
            1080, 720));

        config ??= Config.GetDefaultConfig();
        config.GUI = renderGui;

        var engineCore = new Core(new Renderer(appWindow.Win32Window, config), appWindow.Win32Window.Handle);
        engineCore.OnGUI += appWindow.Render;

        appWindow.Show(ShowWindowCommand.Maximize);
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