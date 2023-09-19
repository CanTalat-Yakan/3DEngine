using System.Runtime.CompilerServices;

using Vortice.Win32;
using static Vortice.Win32.Kernel32;
using static Vortice.Win32.User32;

namespace Engine;

sealed class Program
{
    private Core _engineCore;
    private AppWindow _appWindow;

    [STAThread]
    private static void Main() =>
        new Program().Run();

    private void Loop()
    {
        _engineCore.Frame();
        _appWindow.Render();
    }

    private void Run()
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
        _engineCore = new(win32Window);
        _appWindow = new(win32Window);

        //_engineCore.Renderer.Data.SetSuperSample(true);

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

                    _appWindow?.Dispose();

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
