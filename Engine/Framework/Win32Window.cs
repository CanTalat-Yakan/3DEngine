// Copyright (c) Amer Koleci and contributors.
// Distributed under the MIT license. See the LICENSE file in the project root for more information.

using Vortice.Win32;
using Vortice;

using static Vortice.Win32.User32;

namespace Engine;

public class Win32Window
{
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

        var windowRect = new RawRect(0, 0, Width, Height);
        AdjustWindowRectEx(ref windowRect, style, false, styleEx);

        var windowWidth = Convert.ToInt32(windowRect.Right - windowRect.Left);
        var windowHeight = Convert.ToInt32(windowRect.Bottom - windowRect.Top);

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