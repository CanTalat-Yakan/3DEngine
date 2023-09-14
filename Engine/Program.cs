using ImGuiNET;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Vortice.Win32;

using static Vortice.Win32.Kernel32;
using static Vortice.Win32.User32;

namespace Engine
{
    sealed class MainWindow : AppWindow
    {
        public MainWindow(Win32Window win32window, ID3D11Device device, ID3D11DeviceContext deviceContext) : base(win32window, device, deviceContext) { }

        public override void UpdateImGui()
        {
            base.UpdateImGui();
            ImGui.ShowDemoWindow();
        }
    }

    sealed class Program
    {
        const uint PM_REMOVE = 1;

        Dictionary<IntPtr, AppWindow> windows = new();

        [STAThread]
        private static void Main() =>
            new Program().Run();

        private void Run()
        {
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

            Win32Window win32Window = new(wndClass.ClassName, "3D Engine", 1600, 1000);
            Core engineCore = new(win32Window);
            MainWindow mainWindow = new(win32Window, engineCore.Renderer.Device, engineCore.Renderer.DeviceContext);
            windows.Add(mainWindow.Win32Window.Handle, mainWindow);
            mainWindow.Show();

            bool quitRequested = false;
            while (!quitRequested)
            {
                engineCore.Frame();

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

        private IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
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
