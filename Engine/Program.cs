using System.Runtime.CompilerServices;

using Vortice.Win32;

using static Vortice.Win32.Kernel32;
using static Vortice.Win32.User32;

namespace Engine;

public sealed partial class Program
{
    private Core _engineCore;
    private AppWindow _appWindow;

    [STAThread]
    private static void Main() =>
        new Program().Run(false);

    private void Loop() =>
        _engineCore.Frame();

    public void Run(bool withGui = true, Config config = null)
    {
        CreateWindow(out var win32Window);

        // Instance Engine and AppWindow, then show Window.
        Initialize(win32Window, withGui, config);

        // Create a while loop and break when window requested quit.
        bool quitRequested = false;
        while (!quitRequested)
        {
            if (PeekMessage(out var msg, IntPtr.Zero, 0, 0, 1))
            {
                TranslateMessage(ref msg);
                DispatchMessage(ref msg);

                if (msg.Value == (uint)WindowMessage.Quit)
                {
                    quitRequested = true;

                    Core.Instance?.Dispose();

                    break;
                }
            }

            Loop(); // <-- This is where the loop is handled.
        }
    }

    private void Initialize(Win32Window win32Window, bool withGui, Config config)
    {
        if (config is null)
        {
            config = new();
            config.SetVSync(PresentInterval.Immediate);
            config.SetMSAA(MultiSample.x2);
            config.SetResolutionScale(1);
        }
        config.GUI = withGui;

        _engineCore = new(new Renderer(win32Window, config), win32Window.Handle);
        _appWindow = new(win32Window);

        _engineCore.OnGUI += _appWindow.Render;

        _appWindow.Show(ShowWindowCommand.Maximize);
    }
}

public sealed partial class Program
{
    private void CreateWindow(out Win32Window win32Window, string title = "3D Engine", int width = 1080, int height = 720)
    {
        WNDCLASSEX wndClass = new()
        {
            Size = Unsafe.SizeOf<WNDCLASSEX>(),
            Styles = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW | WindowClassStyles.CS_OWNDC,
            WindowProc = WndProc,
            InstanceHandle = GetModuleHandle(null),
            CursorHandle = LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW),
            BackgroundBrushHandle = IntPtr.Zero,
            IconHandle = IntPtr.Zero,
            ClassName = "WndClass",
        };

        RegisterClassEx(ref wndClass);

        win32Window = new(wndClass.ClassName, title, width, height);
    }

    private IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
    {
        if (_appWindow?.ProcessMessage(msg, wParam, lParam) ?? false)
            return IntPtr.Zero;

        switch ((WindowMessage)msg)
        {
            case WindowMessage.Destroy:
                PostQuitMessage(0);
                break;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }
}