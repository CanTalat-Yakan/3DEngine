using System.Runtime.CompilerServices;

using Engine.Interoperation;

using static Engine.Interoperation.Kernel32;
using static Engine.Interoperation.User32;

namespace Engine.Framework;

public sealed partial class AppWindow
{
    public static Win32Window Win32Window { get; set; }

    public delegate void ResizeEventHandler(int width, int height);
    public static event ResizeEventHandler ResizeEvent;

    public AppWindow(WindowData windowData)
    {
        CreateWindowClass(out var windowClass);

        Win32Window = new(
            windowClass.ClassName,
            windowData.Title,
            windowData.Width,
            windowData.Height);
    }

    public void Show(ShowWindowCommand command = ShowWindowCommand.Normal) =>
        ShowWindow(Win32Window.Handle, command);

    public void Looping(Action onFrame)
    {
        while (true)
        {
            if (!WindowAvailable())
                break;

            onFrame?.Invoke();
        }
    }

    public void Dispose(Action onDispose)
    {
        Win32Window.Destroy();

        onDispose?.Invoke();
    }
}

public sealed partial class AppWindow
{
    public void CreateWindowClass(out WNDCLASSEX windowClass)
    {
        windowClass = new()
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

        RegisterClassEx(ref windowClass);
    }

    public bool WindowAvailable()
    {
        Profiler.Start(out var stopwatch);

        while (PeekMessage(out var msg, IntPtr.Zero, 0, 0, 1))
        {
            if (msg.Value == (uint)WindowMessage.Quit)
                return false;

            TranslateMessage(ref msg);
            DispatchMessage(ref msg);
        }

        Profiler.Stop(stopwatch, "Window");

        return true;
    }
}

public sealed partial class AppWindow
{
    private static IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
    {
        if (ProcessMessage(msg, wParam, lParam))
            return IntPtr.Zero;

        switch ((WindowMessage)msg)
        {
            case WindowMessage.Destroy:
                PostQuitMessage(0);
                break;
        }

        return DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public static bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
    {
        if (GUIInputHandler.Instance?.ProcessMessage((WindowMessage)msg, wParam, lParam) ?? false)
            return true;

        switch ((WindowMessage)msg)
        {
            case WindowMessage.Size:
                switch ((SizeMessage)wParam)
                {
                    case SizeMessage.SIZE_RESTORED:
                    case SizeMessage.SIZE_MAXIMIZED:
                        Win32Window.IsMinimized = false;

                        var lp = (int)lParam;
                        Win32Window.Width = Utils.Loword(lp);
                        Win32Window.Height = Utils.Hiword(lp);

                        ResizeEvent?.Invoke(Win32Window.Width, Win32Window.Height); // <-- This is where resizing is handled.
                        break;
                    case SizeMessage.SIZE_MINIMIZED:
                        Win32Window.IsMinimized = true;
                        break;
                    default:
                        break;
                }
                break;
        }

        return false;
    }
}