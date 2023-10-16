using System.Runtime.CompilerServices;

using Vortice.Win32;

using static Vortice.Win32.Kernel32;
using static Vortice.Win32.User32;

namespace Engine;

public sealed class Program
{
    private Core _engineCore;
    private AppWindow _appWindow;

    [STAThread]
    private static void Main() =>
        new Program().Run();

    private void Loop() =>
        _engineCore.Frame();

    public void Run(bool withGui = true)
    {
        #region // Create Window
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

        Win32Window win32Window = new(wndClass.ClassName, "3D Engine", 1080, 720);
        #endregion

        #region // Instance Engine and AppWindow, then show Window
        Config config = new();
        config.SetVSync(PresentInterval.Immediate);
        config.SetMSAA(MultiSample.x8);
        config.SetResolutionScale(1);

        _engineCore = new(new Renderer(win32Window, config), win32Window.Handle);
        _appWindow = new(win32Window);

        if (withGui)
            _engineCore.OnGui += _appWindow.Render;

        _appWindow.Show(ShowWindowCommand.Maximize);
        #endregion

        #region // LOOP
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
        #endregion
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
