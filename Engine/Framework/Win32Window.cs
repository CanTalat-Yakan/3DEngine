using Win32.Numerics;

using Engine.Interoperation;

using static Engine.Interoperation.User32;

namespace Engine.Framework;

public class Win32Window
{
    public Vortice.Mathematics.SizeI Size => new(Width, Height);

    public string Title;
    public int Width;
    public int Height;
    public IntPtr Handle;
    public bool IsMinimized;

    public Win32Window(string wndClass, string title, int width, int height)
    {
        Title = title;
        Width = width;
        Height = height;

        var screenWidth = GetSystemMetrics(SystemMetrics.SM_CXSCREEN);
        var screenHeight = GetSystemMetrics(SystemMetrics.SM_CYSCREEN);
        var x = (screenWidth - Width) / 2;
        var y = (screenHeight - Height) / 2;

        var style = WindowStyles.WS_OVERLAPPEDWINDOW;
        var styleEx = WindowExStyles.WS_EX_APPWINDOW | WindowExStyles.WS_EX_WINDOWEDGE;

        var windowRect = new Rect(0, 0, Width, Height);
        AdjustWindowRectEx(ref windowRect, style, false, styleEx);

        var windowWidth = windowRect.Right - windowRect.Left;
        var windowHeight = windowRect.Bottom - windowRect.Top;

        var hwnd = CreateWindowEx(
            (int)styleEx, wndClass, Title, (int)style,
            x, y, windowWidth, windowHeight,
            IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

        Handle = hwnd;
    }

    public void Destroy()
    {
        if (Handle != IntPtr.Zero)
        {
            DestroyWindow(Handle);
            Handle = IntPtr.Zero;
        }
    }
}