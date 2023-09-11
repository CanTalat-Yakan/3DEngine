using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Vortice.Win32;
using static Vortice.Win32.Kernel32;
using static Vortice.Win32.User32;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using ImGuiNET;
using Vortice.Direct3D;

namespace VorticeImGui
{
    class MainWindow : AppWindow
    {
        public MainWindow(Win32Window win32window, ID3D11Device device, ID3D11DeviceContext deviceContext) : base(win32window, device, deviceContext) { }

        public override void UpdateImGui()
        {
            base.UpdateImGui();
            ImGui.ShowDemoWindow();
        }
    }

    class Program
    {
        const uint PM_REMOVE = 1;

        [STAThread]
        static void Main()
        {
            new Program().Run();
        }

        bool quitRequested;

        ID3D11Device device;
        ID3D11DeviceContext deviceContext;

        Dictionary<IntPtr, AppWindow> windows = new Dictionary<IntPtr, AppWindow>();

        void Run()
        {
            D3D11.D3D11CreateDevice(null, DriverType.Hardware, DeviceCreationFlags.None, null, out device, out deviceContext);

            var moduleHandle = GetModuleHandle(null);

            var wndClass = new WNDCLASSEX
            {
                Size = Unsafe.SizeOf<WNDCLASSEX>(),
                Styles = WindowClassStyles.CS_HREDRAW | WindowClassStyles.CS_VREDRAW | WindowClassStyles.CS_OWNDC,
                WindowProc = WndProc,
                InstanceHandle = moduleHandle,
                CursorHandle = LoadCursor(IntPtr.Zero, SystemCursor.IDC_ARROW),
                BackgroundBrushHandle = IntPtr.Zero,
                IconHandle = IntPtr.Zero,
                ClassName = "WndClass",
            };

            RegisterClassEx(ref wndClass);

            var win32Window = new Win32Window(wndClass.ClassName, "3D Engine", 1600, 1000);
            var mainWindow = new MainWindow(win32Window, device, deviceContext);
            windows.Add(mainWindow.Win32Window.Handle, mainWindow);
            //Engine.Core engineCore = new(null, win32Window);

            //ShowWindow(win32Window.Handle, ShowWindowCommand.Normal);
            mainWindow.Show();

            while (!quitRequested)
            {
                if (PeekMessage(out var msg, IntPtr.Zero, 0, 0, PM_REMOVE))
                {
                    TranslateMessage(ref msg);
                    DispatchMessage(ref msg);

                    if (msg.Value == (uint)WindowMessage.Quit)
                    {
                        quitRequested = true;
                        break;
                    }
                }

                foreach (var window in windows.Values)
                    window.UpdateAndDraw();
            }
        }

        IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
        {
            AppWindow window;
            windows.TryGetValue(hWnd, out window);

            if (window?.ProcessMessage(msg, wParam, lParam) ?? false)
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
}
