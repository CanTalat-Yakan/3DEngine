using Vortice.Win32;

namespace Engine;

using static Vortice.Win32.User32;

public sealed class Program
{
    private Core _engineCore;

    [STAThread]
    private static void Main() =>
        new Program().Run(false);

    private void Loop() =>
        _engineCore.Frame();

    public void Run(bool withGui = true, Config config = null)
    {
        // Instantiate AppWindow and Engine, then show Window.
        Initialize(withGui, config);

        // Create a while loop and break when the window requested to quit.
        while (true)
        {
            if (PeekMessage(out var msg, IntPtr.Zero, 0, 0, 1))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);

                if (msg.Value == (uint)WindowMessage.Quit)
                {
                    Core.Instance?.Dispose();

                    break;
                }
            }

            Loop(); // <-- This is where the loop is handled.
        }
    }

    private void Initialize(bool withGui, Config config)
    {
        AppWindow appWindow = new();

        appWindow.CreateWindow(out var wndClass);
        appWindow.Initialize(new Win32Window(wndClass.ClassName,
            "3D Engine",
            1080, 720));

        config ??= Config.GetDefaultConfig();
        config.GUI = withGui;

        _engineCore = new Core(new Renderer(appWindow.Win32Window, config), appWindow.Win32Window.Handle);
        _engineCore.OnGUI += appWindow.Render;

        appWindow.Show(ShowWindowCommand.Maximize);
    }
}