using System.Runtime.CompilerServices;

using Vortice.Direct3D;
using Vortice.Direct3D11;
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

        Win32Window win32Window = new(wndClass.ClassName, "3D Engine", 1600, 1000);
        #endregion

        #region // Instance Engine and AppWindow, then show Window
        _engineCore = new(win32Window);

        D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.None, null, out var device, out var deviceContext);
        _appWindow = new(win32Window, device, deviceContext);

        _appWindow.Show();
        #endregion

        #region // LOOP
        const uint PM_REMOVE = 1;

        bool quitRequested = false;
        while (!quitRequested)
        {
            if (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
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

            Loop();
        }
        #endregion
    }

    private void Loop()
    {
        _engineCore.Frame();
        _appWindow.Render();
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
