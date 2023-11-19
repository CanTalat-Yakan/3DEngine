using System.Runtime.CompilerServices;

using ImGuiNET;
using Vortice.Win32;

using static Vortice.Win32.Kernel32;
using static Vortice.Win32.User32;

namespace Engine;

public sealed partial class AppWindow()
{
    public Win32Window Win32Window { get; private set; }

    public delegate void ResizeEventHandler(int width, int height);
    public event ResizeEventHandler ResizeEvent;

    private string _profiler = string.Empty;
    private string _output = string.Empty;

    public void Initialize(WindowData windowData)
    {
        CreateWindow(out var windowClass);

        Win32Window = new Win32Window(
            windowClass.ClassName,
            windowData.Title, 
            windowData.Width, 
            windowData.Height);
    }

    public void Render()
    {
        //ImGui.ShowDemoWindow();

        ImGui.SetNextWindowBgAlpha(0.35f);
        if (ImGui.Begin("Profiler", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (Time.OnFixedFrame)
                _profiler = Profiler.GetAdditionalString();
            ImGui.Text(_profiler);
            ImGui.End();
        }

        ImGui.SetNextWindowBgAlpha(0.35f);
        if (ImGui.Begin("Output", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (Output.GetLogs.Count > 0)
                _output = Output.DequeueLogs() + _output;
            ImGui.Text(_output);
            ImGui.End();
        }
    }

    public void Show(ShowWindowCommand showWindowCommand = ShowWindowCommand.Normal) =>
        ShowWindow(Win32Window.Handle, showWindowCommand);
}

public sealed partial class AppWindow
{
    public bool IsAvailable()
    {
        // Create a while loop and break when the window requested to quit.
        if (PeekMessage(out var msg, IntPtr.Zero, 0, 0, 1))
        {
            TranslateMessage(ref msg);
            DispatchMessage(ref msg);

            if (msg.Value == (uint)WindowMessage.Quit)
                return false;
        }

        return true;
    }

    public void CreateWindow(out WNDCLASSEX wndClass)
    {
        wndClass = new()
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
    }

    public IntPtr WndProc(IntPtr hWnd, uint msg, UIntPtr wParam, IntPtr lParam)
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

    public bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
    {
        Core.Instance?.GUIInputHandler?.ProcessMessage((WindowMessage)msg, wParam, lParam);

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

                        // Invoke the event, passing the parameters
                        ResizeEvent.Invoke(Win32Window.Width, Win32Window.Height);
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