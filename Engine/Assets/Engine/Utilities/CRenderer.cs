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
    internal class CRenderer
    {
        public static CRenderer Instance { get; private set; }

        internal string m_Profile;

        internal ID3D11Device2 m_Device;
        internal ID3D11DeviceContext m_DeviceContext;
        internal SwapChainPanel m_SwapChainPanel;

        internal readonly IDXGISwapChain2 m_SwapChain;

        ID3D11Texture2D m_renderTargetTexture;
        ID3D11RenderTargetView m_renderTargetView;
        ID3D11Texture2D m_depthStencilTexture;
        Texture2DDescription m_depthStencilTextureDescription;
        ID3D11DepthStencilView m_depthStencilView;
        ID3D11BlendState m_blendState;

        internal CRenderer(SwapChainPanel _swapChainPanel)
        {
            #region //Create Instance
            Instance = this;
            #endregion

            #region //Set SwapChainPanel and SizeChanger
            m_SwapChainPanel = _swapChainPanel;
            m_SwapChainPanel.SizeChanged += OnSwapChainPanelSizeChanged;
            #endregion

            #region //Create Buffer Description for swapChain description
            var swapChainDescription = new SwapChainDescription1()
            {
                AlphaMode = AlphaMode.Ignore,
                BufferCount = 2,
                Format = Format.R8G8B8A8_UNorm,
                Height = (int)m_SwapChainPanel.RenderSize.Height,
                Width = (int)m_SwapChainPanel.RenderSize.Width,
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

            m_Device = defaultDevice.QueryInterface<ID3D11Device2>();
            m_DeviceContext = m_Device.ImmediateContext2;

            // Get the Vortice.DXGI factory automatically created when initializing the Direct3D device.
            using (var dxgiDevice3 = m_Device.QueryInterface<IDXGIDevice3>())
            using (IDXGIFactory2 dxgiFactory = dxgiDevice3.GetAdapter().GetParent<IDXGIFactory2>())
            using (IDXGISwapChain1 swapChain1 = dxgiFactory.CreateSwapChainForComposition(dxgiDevice3, swapChainDescription))
                m_SwapChain = swapChain1.QueryInterface<IDXGISwapChain2>();

            using (var nativeObject = ComObject.As<Vortice.WinUI.ISwapChainPanelNative2>(m_SwapChainPanel))
                nativeObject.SetSwapChain(m_SwapChain);
            #endregion

            #region //Create render target view, get back buffer texture before
            m_renderTargetTexture = m_SwapChain.GetBuffer<ID3D11Texture2D>(0);
            m_renderTargetView = m_Device.CreateRenderTargetView(m_renderTargetTexture);
            #endregion

            #region //Create depth stencil view
            m_depthStencilTextureDescription = new Texture2DDescription
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
            using (m_depthStencilTexture = m_Device.CreateTexture2D(m_depthStencilTextureDescription))
                m_depthStencilView = m_Device.CreateDepthStencilView(m_depthStencilTexture);

            DepthStencilDescription desc = new DepthStencilDescription()
            {
                DepthEnable = true,
                DepthFunc = ComparisonFunction.Less,
                DepthWriteMask = DepthWriteMask.All,
            };
            ID3D11DepthStencilState state = m_Device.CreateDepthStencilState(desc);
            m_DeviceContext.OMSetDepthStencilState(state);
            m_DeviceContext.OMSetRenderTargets(m_renderTargetView, m_depthStencilView);
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
            m_blendState = m_Device.CreateBlendState(blendStateDesc);
            m_DeviceContext.OMSetBlendState(m_blendState);
            #endregion

            #region //Set ViewPort
            m_DeviceContext.RSSetViewport(0, 0, (int)m_SwapChainPanel.ActualWidth, (int)m_SwapChainPanel.ActualHeight);
            #endregion
        }

        public void Dispose()
        {
            m_Device.Dispose();
            m_DeviceContext.Dispose();
            m_SwapChain.Dispose();
            m_depthStencilView.Dispose();
            m_depthStencilTexture.Dispose();
            m_renderTargetView.Dispose();
            m_renderTargetTexture.Dispose();
            m_blendState.Dispose();
        }



        internal void Clear()
        {
            var col = new Color4(0.15f, 0.15f, 0.15f, 1);
            m_DeviceContext.ClearRenderTargetView(m_renderTargetView, col);
            m_DeviceContext.ClearDepthStencilView(m_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
            m_DeviceContext.OMSetRenderTargets(m_renderTargetView, m_depthStencilView);
        }

        internal void Present()
        {
            m_SwapChain.Present(0, PresentFlags.None);

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

            m_DeviceContext.RSSetState(m_Device.CreateRasterizerState(rasterizerDesc));
        }
        internal void SetWireframe()
        {
            var rasterizerDescWireframe = new RasterizerDescription()
            {
                FillMode = FillMode.Wireframe,
                CullMode = CullMode.None,
            };

            m_DeviceContext.RSSetState(m_Device.CreateRasterizerState(rasterizerDescWireframe));
        }



        internal void RenderMesh(ID3D11Buffer _vertexBuffer, int _vertexStride, ID3D11Buffer _indexBuffer, int _indexCount)
        {
            m_DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
            m_DeviceContext.IASetVertexBuffer(0, _vertexBuffer, _vertexStride, 0);
            m_DeviceContext.IASetIndexBuffer(_indexBuffer, Format.R16_UInt, 0);
            m_DeviceContext.OMSetBlendState(m_blendState);
            m_DeviceContext.DrawIndexed(_indexCount, 0, 0);
        }



        internal void OnSwapChainPanelSizeChanged(object sender, SizeChangedEventArgs e)
        {
            var newSize = new SizeI((int)e.NewSize.Width, (int)e.NewSize.Height);

            m_renderTargetView.Dispose();
            m_renderTargetTexture.Dispose();
            m_depthStencilView.Dispose();
            m_depthStencilTexture.Dispose();

            m_Profile = "Resolution: " + "\n" + ((int)e.NewSize.Width).ToString() + ":" + ((int)e.NewSize.Height).ToString();

            m_SwapChain.ResizeBuffers(
              m_SwapChain.Description.BufferCount,
              Math.Max(1, (int)e.NewSize.Width),
              Math.Max(1, (int)e.NewSize.Height),
              m_SwapChain.Description1.Format,
              m_SwapChain.Description1.Flags);

            m_renderTargetTexture = m_SwapChain.GetBuffer<ID3D11Texture2D>(0);
            m_renderTargetView = m_Device.CreateRenderTargetView(m_renderTargetTexture);

            m_depthStencilTextureDescription.Width = Math.Max(1, (int)e.NewSize.Width);
            m_depthStencilTextureDescription.Height = Math.Max(1, (int)e.NewSize.Height);
            using (m_depthStencilTexture = m_Device.CreateTexture2D(m_depthStencilTextureDescription))
                m_depthStencilView = m_Device.CreateDepthStencilView(m_depthStencilTexture);


            m_SwapChain.SourceSize = newSize;

            m_DeviceContext.RSSetViewport(0, 0, (int)e.NewSize.Width, (int)e.NewSize.Height);
        }
    }
}
