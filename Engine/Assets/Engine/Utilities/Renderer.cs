using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SharpGen.Runtime;
using System;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Utilities
{
    internal class Renderer
    {
        public static Renderer Instance { get; private set; }

        internal string profile;

        internal ID3D11Device2 device;
        internal ID3D11DeviceContext deviceContext;
        internal SwapChainPanel swapChainPanel;

        internal readonly IDXGISwapChain2 swapChain;

        ID3D11Texture2D renderTargetTexture;
        ID3D11RenderTargetView renderTargetView;
        ID3D11Texture2D depthStencilTexture;
        Texture2DDescription depthStencilTextureDescription;
        ID3D11DepthStencilView depthStencilView;
        ID3D11BlendState blendState;

        internal Renderer(SwapChainPanel _swapChainPanel)
        {
            #region //Create Instance
            Instance = this;
            #endregion

            #region //Set SwapChainPanel and SizeChanger
            swapChainPanel = _swapChainPanel;
            swapChainPanel.SizeChanged += OnSwapChainPanelSizeChanged;
            #endregion

            #region //Create Buffer Description for swapChain description
            var swapChainDescription = new SwapChainDescription1()
            {
                AlphaMode = AlphaMode.Ignore,
                BufferCount = 2,
                Format = Format.R8G8B8A8_UNorm,
                Height = (int)swapChainPanel.RenderSize.Height,
                Width = (int)swapChainPanel.RenderSize.Width,
                SampleDescription = new SampleDescription(1, 0),
                Scaling = Scaling.Stretch,
                Stereo = false,
                SwapEffect = SwapEffect.FlipSequential,
                BufferUsage = Usage.RenderTargetOutput
            };
            #endregion

            #region //Create device, device context & swap chain
            D3D11.D3D11CreateDevice(
                null,
                DriverType.Hardware,
                DeviceCreationFlags.BgraSupport,
                new[]
                {
                    FeatureLevel.Level_11_1,
                    FeatureLevel.Level_11_0,
                },
                out var defaultDevice);

            device = defaultDevice.QueryInterface<ID3D11Device2>();
            deviceContext = device.ImmediateContext2;

            // Get the Vortice.DXGI factory automatically created when initializing the Direct3D device.
            using (var dxgiDevice3 = device.QueryInterface<IDXGIDevice3>())
            using (IDXGIFactory2 dxgiFactory = dxgiDevice3.GetAdapter().GetParent<IDXGIFactory2>())
            using (IDXGISwapChain1 swapChain1 = dxgiFactory.CreateSwapChainForComposition(dxgiDevice3, swapChainDescription))
                swapChain = swapChain1.QueryInterface<IDXGISwapChain2>();

            using (var nativeObject = ComObject.As<Vortice.WinUI.ISwapChainPanelNative2>(swapChainPanel))
                nativeObject.SetSwapChain(swapChain);
            #endregion

            #region //Create render target view, get back buffer texture before
            renderTargetTexture = swapChain.GetBuffer<ID3D11Texture2D>(0);
            renderTargetView = device.CreateRenderTargetView(renderTargetTexture);
            #endregion

            #region //Create depth stencil view
            depthStencilTextureDescription = new Texture2DDescription
            {
                Format = Format.D32_Float,
                ArraySize = 1,
                MipLevels = 0,
                Width = swapChainDescription.Width,
                Height = swapChainDescription.Height,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CPUAccessFlags = CpuAccessFlags.None,
                MiscFlags = ResourceOptionFlags.None
            };
            using (depthStencilTexture = device.CreateTexture2D(depthStencilTextureDescription))
                depthStencilView = device.CreateDepthStencilView(depthStencilTexture);

            DepthStencilDescription desc = new DepthStencilDescription()
            {
                DepthEnable = true,
                DepthFunc = ComparisonFunction.Less,
                DepthWriteMask = DepthWriteMask.All,
            };
            ID3D11DepthStencilState state = device.CreateDepthStencilState(desc);
            deviceContext.OMSetDepthStencilState(state);
            deviceContext.OMSetRenderTargets(renderTargetView, depthStencilView);
            #endregion

            #region //Create rasterizer state
            SetSolid();
            #endregion

            #region //Create Blend State
            var blendStateDesc = new BlendDescription();
            var renTarDesc = new RenderTargetBlendDescription()
            {
                IsBlendEnabled = true,
                SourceBlend = Blend.SourceAlpha,
                DestinationBlend = Blend.InverseSourceAlpha,
                BlendOperation = BlendOperation.Add,
                SourceBlendAlpha = Blend.One,
                DestinationBlendAlpha = Blend.Zero,
                BlendOperationAlpha = BlendOperation.Add,
                RenderTargetWriteMask = ColorWriteEnable.All
            };

            blendStateDesc.RenderTarget[0] = renTarDesc;
            blendState = device.CreateBlendState(blendStateDesc);
            deviceContext.OMSetBlendState(blendState);
            #endregion

            #region //Set ViewPort
            deviceContext.RSSetViewport(0, 0, (int)swapChainPanel.ActualWidth, (int)swapChainPanel.ActualHeight);
            #endregion
        }

        public void Dispose()
        {
            device.Dispose();
            deviceContext.Dispose();
            swapChain.Dispose();
            depthStencilView.Dispose();
            depthStencilTexture.Dispose();
            renderTargetView.Dispose();
            renderTargetTexture.Dispose();
            blendState.Dispose();
        }



        internal void Clear()
        {
            var col = new Color4(0.15f, 0.15f, 0.15f, 1);
            deviceContext.ClearRenderTargetView(renderTargetView, col);
            deviceContext.ClearDepthStencilView(depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            deviceContext.OMSetRenderTargets(renderTargetView, depthStencilView);
        }

        internal void Present()
        {
            swapChain.Present(0, PresentFlags.None);

            //int syncInterval = 1;
            //PresentFlags presentFlags = PresentFlags.None;
            //syncInterval = 0;
            //presentFlags = PresentFlags.AllowTearing;

            //Result result = m_SwapChain.Present(syncInterval, presentFlags);
        }



        internal void SetSolid()
        {
            var rasterizerDesc = new RasterizerDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
            };

            deviceContext.RSSetState(device.CreateRasterizerState(rasterizerDesc));
        }
        internal void SetWireframe()
        {
            var rasterizerDescWireframe = new RasterizerDescription()
            {
                FillMode = FillMode.Wireframe,
                CullMode = CullMode.None,
            };

            deviceContext.RSSetState(device.CreateRasterizerState(rasterizerDescWireframe));
        }



        internal void RenderMesh(ID3D11Buffer _vertexBuffer, int _vertexStride, ID3D11Buffer _indexBuffer, int _indexCount)
        {
            deviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            deviceContext.IASetVertexBuffer(0, _vertexBuffer, _vertexStride, 0);
            deviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            deviceContext.OMSetBlendState(blendState);
            deviceContext.DrawIndexed(_indexCount, 0, 0);
        }



        internal void OnSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = new SizeI((int)e.NewSize.Width, (int)e.NewSize.Height);

            renderTargetView.Dispose();
            renderTargetTexture.Dispose();
            depthStencilView.Dispose();
            depthStencilTexture.Dispose();

            profile = "Resolution: " + "\n" + ((int)e.NewSize.Width).ToString() + ":" + ((int)e.NewSize.Height).ToString();

            swapChain.ResizeBuffers(
              swapChain.Description.BufferCount,
              Math.Max(1, (int)e.NewSize.Width),
              Math.Max(1, (int)e.NewSize.Height),
              swapChain.Description1.Format,
              swapChain.Description1.Flags);

            renderTargetTexture = swapChain.GetBuffer<ID3D11Texture2D>(0);
            renderTargetView = device.CreateRenderTargetView(renderTargetTexture);

            depthStencilTextureDescription.Width = Math.Max(1, (int)e.NewSize.Width);
            depthStencilTextureDescription.Height = Math.Max(1, (int)e.NewSize.Height);
            using (depthStencilTexture = device.CreateTexture2D(depthStencilTextureDescription))
                depthStencilView = device.CreateDepthStencilView(depthStencilTexture);


            swapChain.SourceSize = newSize;

            deviceContext.RSSetViewport(0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height);
        }
    }
}
