using System.Runtime.CompilerServices;

using ImGuiNET;
using Vortice.Win32;

using static Vortice.Win32.Kernel32;
using static Vortice.Win32.User32;

namespace Engine;

public sealed partial class AppWindow()
{
    public Win32Window Win32Window { get; private set; }

    private GuiRenderer _imGuiRenderer;
    private GuiInputHandler _imGuiInputHandler;

    private IntPtr _imGuiContext;

    private string _profiler = string.Empty;
    private string _output = string.Empty;

    public void Initialize(Win32Window win32Window)
    {
        Win32Window = win32Window;

        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        _imGuiRenderer = new();
        _imGuiInputHandler = new(Win32Window.Handle);
    }

    public void Render()
    {
        _imGuiRenderer.Update(_imGuiContext, Core.Instance.Renderer.Size);
        _imGuiInputHandler.Update();

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

    public void Resize() =>
        Core.Instance.Renderer.Resize(Win32Window.Width, Win32Window.Height);

    public void Show(ShowWindowCommand showWindowCommand = ShowWindowCommand.Normal) =>
        ShowWindow(Win32Window.Handle, showWindowCommand);
}

public sealed partial class AppWindow
{
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
        if (Core.Instance is null)
            return false;

        ImGui.SetCurrentContext(_imGuiContext);
        if (_imGuiInputHandler is not null && _imGuiInputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam))
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

                        Resize(); // <-- This is where resizing is handled.
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