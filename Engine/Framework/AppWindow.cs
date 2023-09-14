using ImGuiNET;
using System.Diagnostics;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.Win32;
using VorticeImGui;

namespace Engine
{
    class AppWindow
    {
        public Win32Window Win32Window;

        ID3D11Device device;
        ID3D11DeviceContext deviceContext;
        IDXGISwapChain swapChain;
        ID3D11Texture2D backBuffer;
        ID3D11RenderTargetView renderView;

        Format format = Format.R8G8B8A8_UNorm;

        ImGuiRenderer imGuiRenderer;
        ImGuiRenderer2 imGuiRenderer2;
        ImGuiInputHandler imGuiInputHandler;
        Stopwatch stopwatch = Stopwatch.StartNew();
        TimeSpan lastFrameTime;

        IntPtr imGuiContext;

        public AppWindow(Win32Window win32window, ID3D11Device device, ID3D11DeviceContext deviceContext)
        {
            Win32Window = win32window;
            this.device = device;
            this.deviceContext = deviceContext;

            imGuiContext = ImGui.CreateContext();
            ImGui.SetCurrentContext(imGuiContext);

            imGuiRenderer2 = new ImGuiRenderer2(this.device, this.deviceContext);
            imGuiRenderer = new ImGuiRenderer(this.device, this.deviceContext);
            imGuiInputHandler = new ImGuiInputHandler(Win32Window.Handle);

            ImGui.GetIO().DisplaySize = new Vector2(Win32Window.Width, Win32Window.Height);
        }

        public void Show() =>
            User32.ShowWindow(Win32Window.Handle, ShowWindowCommand.Normal);

        public virtual bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            ImGui.SetCurrentContext(imGuiContext);
            if (imGuiInputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam))
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

                            Core.Instance.Renderer.OnSwapChainSizeChanged(Win32Window.Width, Win32Window.Height);
                            resize(); // <-- This is where resizing is handled
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

        public void UpdateAndDraw()
        {
            UpdateImGui();
            render();
        }

        void resize()
        {
            if (renderView == null)//first show
            {
                var dxgiFactory = device.QueryInterface<IDXGIDevice>().GetParent<IDXGIAdapter>().GetParent<IDXGIFactory>();

                var swapChainDesc = new SwapChainDescription()
                {
                    BufferCount = 1,
                    BufferDescription = new ModeDescription(Win32Window.Width, Win32Window.Height, format),
                    Windowed = true,
                    OutputWindow = Win32Window.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    BufferUsage = Usage.RenderTargetOutput
                };

                swapChain = dxgiFactory.CreateSwapChain(device, swapChainDesc);
                dxgiFactory.MakeWindowAssociation(Win32Window.Handle, WindowAssociationFlags.IgnoreAll);

                backBuffer = swapChain.GetBuffer<ID3D11Texture2D>(0);
                renderView = device.CreateRenderTargetView(backBuffer);
            }
            else
            {
                renderView.Dispose();
                backBuffer.Dispose();

                swapChain.ResizeBuffers(1, Win32Window.Width, Win32Window.Height, format, SwapChainFlags.None);

                backBuffer = swapChain.GetBuffer<ID3D11Texture2D1>(0);
                renderView = device.CreateRenderTargetView(backBuffer);

                UpdateAndDraw();
            }
        }

        public virtual void UpdateImGui()
        {
            ImGui.SetCurrentContext(imGuiContext);
            var io = ImGui.GetIO();

            var now = stopwatch.Elapsed;
            var delta = now - lastFrameTime;
            lastFrameTime = now;
            io.DeltaTime = (float)delta.TotalSeconds;

            io.DisplaySize = new Vector2(Win32Window.Width, Win32Window.Height);

            imGuiInputHandler.Update();

            ImGui.NewFrame();
        }

        void render()
        {
            ImGui.Render();

            var dc = deviceContext;
            dc.ClearRenderTargetView(renderView, new Color4(0, 0, 0));
            dc.OMSetRenderTargets(renderView);
            dc.RSSetViewport(0, 0, Win32Window.Width, Win32Window.Height);
            
            imGuiRenderer.Render(ImGui.GetDrawData());
            //imGuiRenderer2.Render(ImGui.GetDrawData());
            DoRender();

            swapChain.Present(0, PresentFlags.None);
        }

        public virtual void DoRender() { }
    }
}
