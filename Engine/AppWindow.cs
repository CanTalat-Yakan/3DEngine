using ImGuiNET;
using Vortice.Win32;

namespace Engine;

class AppWindow
{
    private ImGuiRenderer _imGuiRenderer;
    private ImGuiInputHandler _imGuiInputHandler;

    private IntPtr _imGuiContext;

    private Win32Window _win32Window;

    private string _profiler = "";

    public AppWindow(Win32Window win32window)
    {
        _win32Window = win32window;

        _imGuiContext = ImGui.CreateContext();
        ImGui.SetCurrentContext(_imGuiContext);

        _imGuiRenderer = new();
        _imGuiInputHandler = new(_win32Window.Handle);
    }

    public void Show(ShowWindowCommand command = ShowWindowCommand.Normal) =>
        User32.ShowWindow(_win32Window.Handle, command);
    public void Render()
    {
        _imGuiRenderer.Update(_imGuiContext, Core.Instance.Renderer.Size);
        _imGuiInputHandler.Update(Core.Instance.Renderer.Config.SuperSample);

        ImGui.ShowDemoWindow();

        ImGui.SetNextWindowBgAlpha(0.35f);
        if (ImGui.Begin("Profiler", ImGuiWindowFlags.NoDecoration | ImGuiWindowFlags.AlwaysAutoResize))
        {
            if (Time.TimeStepElapsed)
                _profiler = Profiler.GetString();
            ImGui.Text(_profiler);
            ImGui.End();
        }
    }

    public void Resize() =>
        Core.Instance.Renderer.Resize(_win32Window.Width, _win32Window.Height);

    public bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
    {
        ImGui.SetCurrentContext(_imGuiContext);
        if (_imGuiInputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam))
            return true;

        switch ((WindowMessage)msg)
        {
            case WindowMessage.Size:
                switch ((SizeMessage)wParam)
                {
                    case SizeMessage.SIZE_RESTORED:
                    case SizeMessage.SIZE_MAXIMIZED:
                        _win32Window.IsMinimized = false;

                        var lp = (int)lParam;
                        _win32Window.Width = Utils.Loword(lp);
                        _win32Window.Height = Utils.Hiword(lp);

                        Resize(); // <-- This is where resizing is handled.
                        break;
                    case SizeMessage.SIZE_MINIMIZED:
                        _win32Window.IsMinimized = true;
                        break;
                    default:
                        break;
                }
                break;
        }

        return false;
    }
}
