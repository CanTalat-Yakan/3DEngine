using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Text;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;
using Vortice.Win32;
using VorticeImGui;

namespace VorticeImGui
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
        ImGuiInputHandler imguiInputHandler;
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

            imGuiRenderer = new ImGuiRenderer(this.device, this.deviceContext);
            imguiInputHandler = new ImGuiInputHandler(Win32Window.Handle);

            ImGui.GetIO().DisplaySize = new Vector2(Win32Window.Width, Win32Window.Height);
        }

        public void Show()
        {
            User32.ShowWindow(Win32Window.Handle, ShowWindowCommand.Normal);
        }

        public virtual bool ProcessMessage(uint msg, UIntPtr wParam, IntPtr lParam)
        {
            ImGui.SetCurrentContext(imGuiContext);
            if (imguiInputHandler.ProcessMessage((WindowMessage)msg, wParam, lParam))
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

                var swapchainDesc = new SwapChainDescription()
                {
                    BufferCount = 1,
                    BufferDescription = new ModeDescription(Win32Window.Width, Win32Window.Height, format),
                    Windowed = true,
                    OutputWindow = Win32Window.Handle,
                    SampleDescription = new SampleDescription(1, 0),
                    SwapEffect = SwapEffect.Discard,
                    BufferUsage = Vortice.DXGI.Usage.RenderTargetOutput
                };

                swapChain = dxgiFactory.CreateSwapChain(device, swapchainDesc);
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

            imguiInputHandler.Update();

            ImGui.NewFrame();
        }

        void render()
        {
            ImGui.Render();

            var dc = deviceContext;
            dc.ClearRenderTargetView(renderView, new Color4(0, 0, 0));
            dc.OMSetRenderTargets(renderView);
            dc.RSSetViewport(0, 0, Win32Window.Width, Win32Window.Height);
            
            ImGui.GetIO().DisplaySize = new Vector2(Win32Window.Width, Win32Window.Height);

            imGuiRenderer.Render(ImGui.GetDrawData());
            DoRender();

            swapChain.Present(0, PresentFlags.None);
        }

        public virtual void DoRender() { }
    }
}
