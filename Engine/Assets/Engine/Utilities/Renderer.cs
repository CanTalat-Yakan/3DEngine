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

        public string Profile;

        public ID3D11Device2 Device { get; private set; }
        public ID3D11DeviceContext DeviceContext { get; private set; }
        public SwapChainPanel SwapChainPanel { get; private set; }

        private readonly IDXGISwapChain2 _swapChain;

        private ID3D11Texture2D _renderTargetTexture;
        private ID3D11RenderTargetView _renderTargetView;
        private ID3D11Texture2D _depthStencilTexture;
        private Texture2DDescription _depthStencilTextureDescription;
        private ID3D11DepthStencilView _depthStencilView;
        private ID3D11BlendState _blendState;

        public Renderer(SwapChainPanel swapChainPanel)
        {
            #region //Create Instance
            if (Instance is null)
                Instance = this;
            #endregion

            #region //Set SwapChainPanel and SizeChanger
            SwapChainPanel = swapChainPanel;
            SwapChainPanel.SizeChanged += OnSwapChainPanelSizeChanged;
            #endregion

            #region //Create Buffer Description for swapChain description
            var swapChainDescription = new SwapChainDescription1()
            {
                AlphaMode = AlphaMode.Ignore,
                BufferCount = 2,
                Format = Format.R8G8B8A8_UNorm,
                Height = (int)SwapChainPanel.RenderSize.Height,
                Width = (int)SwapChainPanel.RenderSize.Width,
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

            Device = defaultDevice.QueryInterface<ID3D11Device2>();
            DeviceContext = Device.ImmediateContext2;

            // Get the Vortice.DXGI factory automatically created when initializing the Direct3D device.
            using (var dxgiDevice3 = Device.QueryInterface<IDXGIDevice3>())
            using (IDXGIFactory2 dxgiFactory = dxgiDevice3.GetAdapter().GetParent<IDXGIFactory2>())
            using (IDXGISwapChain1 swapChain1 = dxgiFactory.CreateSwapChainForComposition(dxgiDevice3, swapChainDescription))
                _swapChain = swapChain1.QueryInterface<IDXGISwapChain2>();

            using (var nativeObject = ComObject.As<Vortice.WinUI.ISwapChainPanelNative2>(this.SwapChainPanel))
                nativeObject.SetSwapChain(_swapChain);
            #endregion

            #region //Create render target view, get back buffer texture before
            _renderTargetTexture = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            _renderTargetView = Device.CreateRenderTargetView(_renderTargetTexture);
            #endregion

            #region //Create depth stencil view
            _depthStencilTextureDescription = new Texture2DDescription
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
            using (_depthStencilTexture = Device.CreateTexture2D(_depthStencilTextureDescription))
                _depthStencilView = Device.CreateDepthStencilView(_depthStencilTexture);

            DepthStencilDescription desc = new DepthStencilDescription()
            {
                DepthEnable = true,
                DepthFunc = ComparisonFunction.Less,
                DepthWriteMask = DepthWriteMask.All,
            };
            ID3D11DepthStencilState state = Device.CreateDepthStencilState(desc);
            DeviceContext.OMSetDepthStencilState(state);
            DeviceContext.OMSetRenderTargets(_renderTargetView, _depthStencilView);
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
            _blendState = Device.CreateBlendState(blendStateDesc);
            unsafe
            {
                DeviceContext.OMSetBlendState(_blendState);
            }
            #endregion

            #region //Set ViewPort
            DeviceContext.RSSetViewport(0, 0, (int)this.SwapChainPanel.ActualWidth, (int)this.SwapChainPanel.ActualHeight);
            #endregion
        }

        public void Dispose()
        {
            Device.Dispose();
            DeviceContext.Dispose();
            _swapChain.Dispose();
            _depthStencilView.Dispose();
            _depthStencilTexture.Dispose();
            _renderTargetView.Dispose();
            _renderTargetTexture.Dispose();
            _blendState.Dispose();
        }

        public void Clear()
        {
            var col = new Color4(0.15f, 0.15f, 0.15f, 1);
            DeviceContext.ClearRenderTargetView(_renderTargetView, col);
            DeviceContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            DeviceContext.OMSetRenderTargets(_renderTargetView, _depthStencilView);
        }

        public void Present()
        {
            _swapChain.Present(0, PresentFlags.None);

            //int syncInterval = 1;
            //PresentFlags presentFlags = PresentFlags.None;
            //syncInterval = 0;
            //presentFlags = PresentFlags.AllowTearing;

            //Result result = m_SwapChain.Present(syncInterval, presentFlags);
        }

        public void SetSolid()
        {
            var rasterizerDesc = new RasterizerDescription()
            {
                FillMode = FillMode.Solid,
                CullMode = CullMode.Back,
            };

            DeviceContext.RSSetState(Device.CreateRasterizerState(rasterizerDesc));
        }

        public void SetWireframe()
        {
            var rasterizerDescWireframe = new RasterizerDescription()
            {
                FillMode = FillMode.Wireframe,
                CullMode = CullMode.None,
            };

            DeviceContext.RSSetState(Device.CreateRasterizerState(rasterizerDescWireframe));
        }

        public void RenderMesh(ID3D11Buffer _vertexBuffer, int _vertexStride, ID3D11Buffer _indexBuffer, int _indexCount)
        {
            DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            DeviceContext.IASetVertexBuffer(0, _vertexBuffer, _vertexStride, 0);
            DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            unsafe
            {
                DeviceContext.OMSetBlendState(_blendState);
            }
            DeviceContext.DrawIndexed(_indexCount, 0, 0);
        }

        public void OnSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = new SizeI((int)e.NewSize.Width, (int)e.NewSize.Height);

            _renderTargetView.Dispose();
            _renderTargetTexture.Dispose();
            _depthStencilView.Dispose();
            _depthStencilTexture.Dispose();

            Profile = "Resolution: " + "\n" + ((int)e.NewSize.Width).ToString() + ":" + ((int)e.NewSize.Height).ToString();

            _swapChain.ResizeBuffers(
              _swapChain.Description.BufferCount,
              Math.Max(1, (int)e.NewSize.Width),
              Math.Max(1, (int)e.NewSize.Height),
              _swapChain.Description1.Format,
              _swapChain.Description1.Flags);

            _renderTargetTexture = _swapChain.GetBuffer<ID3D11Texture2D>(0);
            _renderTargetView = Device.CreateRenderTargetView(_renderTargetTexture);

            _depthStencilTextureDescription.Width = Math.Max(1, (int)e.NewSize.Width);
            _depthStencilTextureDescription.Height = Math.Max(1, (int)e.NewSize.Height);
            using (_depthStencilTexture = Device.CreateTexture2D(_depthStencilTextureDescription))
                _depthStencilView = Device.CreateDepthStencilView(_depthStencilTexture);


            _swapChain.SourceSize = newSize;

            DeviceContext.RSSetViewport(0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height);
        }
    }
}
