using Vortice.Win32;

namespace Engine;

class AppWindow
{
    private ImGuiRenderer _imGuiRenderer;
    private ImGuiInputHandler _imGuiInputHandler;

    private IntPtr _imGuiContext;

    private Win32Window _win32Window;

    public AppWindow(Win32Window win32window)
    {
        _win32Window = win32window;

        _imGuiContext = ImGuiNET.ImGui.CreateContext();
        ImGuiNET.ImGui.SetCurrentContext(_imGuiContext);

        _imGuiRenderer = new ImGuiRenderer(_win32Window);
        _imGuiInputHandler = new ImGuiInputHandler(_win32Window.Handle);

        ImGuiNET.ImGui.GetIO().DisplaySize = new Vector2(_win32Window.Width, _win32Window.Height);
    }

    public void Show(ShowWindowCommand command = ShowWindowCommand.Normal) =>
        User32.ShowWindow(_win32Window.Handle, command);

    public void Render()
    {
        _imGuiRenderer.Update(_imGuiContext, new Vector2(_win32Window.Width, _win32Window.Height));
        _imGuiInputHandler.Update();

        ImGuiNET.ImGui.ShowDemoWindow();

        ImGuiNET.ImGui.Render();
        _imGuiRenderer.Render();        

        _imGuiRenderer.Present();
    }

    public void Resize()
    {
        Core.Instance.Renderer.Resize(_win32Window.Width, _win32Window.Height);
        //Core.Instance.Frame();

        _imGuiRenderer.Resize();
        //Render();
    }

    public bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
    {
        ImGuiNET.ImGui.SetCurrentContext(_imGuiContext);
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

    public void Dispose()
    {
        Core.Instance?.Dispose();
        _imGuiRenderer?.Dispose();
    }
}
