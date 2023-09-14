using ImGui;

using Vortice.Direct3D11;
using Vortice.Win32;

namespace Engine;

class AppWindow
{
    private ID3D11Device _device;
    private ID3D11DeviceContext _deviceContext;

    private ImGuiRenderer _imGuiRenderer;
    private ImGuiInputHandler _imGuiInputHandler;

    private IntPtr _imGuiContext;

    private Win32Window _win32Window;

    public AppWindow(Win32Window win32window, ID3D11Device device, ID3D11DeviceContext deviceContext)
    {
        _win32Window = win32window;

        _device = device;
        _deviceContext = deviceContext;

        _imGuiContext = ImGuiNET.ImGui.CreateContext();
        ImGuiNET.ImGui.SetCurrentContext(_imGuiContext);

        _imGuiRenderer = new ImGuiRenderer(_device, _deviceContext);
        _imGuiInputHandler = new ImGuiInputHandler(_win32Window.Handle);

        ImGuiNET.ImGui.GetIO().DisplaySize = new Vector2(_win32Window.Width, _win32Window.Height);
    }

    public void Show() =>
        User32.ShowWindow(_win32Window.Handle, ShowWindowCommand.Normal);

    public void Render()
    {
        _imGuiRenderer.Update(_imGuiContext, new Vector2(_win32Window.Width, _win32Window.Height));
        _imGuiInputHandler.Update();

        ImGuiNET.ImGui.ShowDemoWindow();

        ImGuiNET.ImGui.Render();
        _imGuiRenderer.Render();        

        _imGuiRenderer.Present();        
    }

    public bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
    {
        ImGuiNET.ImGui.SetCurrentContext(_imGuiContext);
        if (_imGuiInputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam))
            return true;

        if (!_imGuiRenderer.IsRendering)
            _imGuiRenderer.InitializeSwapChain(_win32Window);

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

                        Core.Instance.Renderer.OnSwapChainSizeChanged(_win32Window.Width, _win32Window.Height);
                        Core.Instance.Frame();

                        _imGuiRenderer.Resize(); // <-- This is where resizing is handled
                        Render();
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
