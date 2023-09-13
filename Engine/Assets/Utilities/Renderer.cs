using SharpGen.Runtime;
using System.Diagnostics;
using System.Drawing;
using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Utilities;

public sealed class Renderer
{
    public static Renderer Instance { get; private set; }

    public bool IsRendering { get => _renderTargetView.NativePointer is not 0; }

    public Size Size { get; private set; }

    public ID3D11Device2 Device { get; private set; }
    public ID3D11DeviceContext DeviceContext { get; private set; }

    public IDXGISwapChain2 SwapChain { get => _swapChain; }
    private IDXGISwapChain2 _swapChain;

    private ID3D11Texture2D _renderTargetTexture;
    private ID3D11RenderTargetView _renderTargetView;
    private ID3D11Texture2D _depthStencilTexture;
    private Texture2DDescription _depthStencilTextureDescription;
    private ID3D11DepthStencilView _depthStencilView;
    private ID3D11BlendState _blendState;

#if !EDITOR
    private Win32Window _win32Window;

    public Renderer(Win32Window win32Window)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        // Store the instance of Win32Window.
        if (win32Window is not null)
            _win32Window = win32Window;
        else
            throw new Exception("""
                An invalid or null Win32Window instance was passed to the Renderer. 
                Please ensure that you provide a valid Win32Window object when constructing the Renderer.
                """);

        // Set the size.
        Size = new Size(_win32Window.Width, _win32Window.Height);

        var result = Initialization(true);
        if (result.Failure)
            throw new Exception(result.Description);
    }
#endif

    public Renderer(int sizeX = 640, int sizeY = 480)
    {
        if (Instance is null)
            Instance = this;

        Size = new Size(
            Math.Max(640, sizeX), 
            Math.Max(480, sizeY));
    }

    public Result Initialization(bool forHwnd = false)
    {
        #region //Create device, device context & swap chain with result
        // Create a Direct3D 11 device.
        var result = D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport | DeviceCreationFlags.Debug,
            new[]
            {
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
            },
            out var defaultDevice);

        // Check if creating the device was successful.
        if (result.Failure)
            return result;

        // Assign the device to a variable.
        Device = defaultDevice.QueryInterface<ID3D11Device2>();
        // Get the immediate context of the device.
        DeviceContext = Device.ImmediateContext2;

        // Initialize the SwapChainDescription structure.
        SwapChainDescription1 swapChainDescription1 = new()
        {
            AlphaMode = AlphaMode.Ignore,
            BufferCount = 2,
            Format = Format.R8G8B8A8_UNorm,
            Width = Size.Width,
            Height = Size.Height,
            SampleDescription = new(1, 0),
            Scaling = Scaling.Stretch,
            Stereo = false,
            SwapEffect = SwapEffect.FlipSequential,
            BufferUsage = Usage.RenderTargetOutput
        };
        
        try
        {
            // Obtain instance of the IDXGIDevice3 interface from the Direct3D device.
            IDXGIDevice3 dxgiDevice3 = Device.QueryInterface<IDXGIDevice3>();
            // Obtain instance of the IDXGIFactory2 interface from the DXGI device.
            IDXGIFactory2 dxgiFactory2 = dxgiDevice3.GetParent<IDXGIAdapter>().GetParent<IDXGIFactory2>();
            // Creates a swap chain using the swap chain description.
            IDXGISwapChain1 swapChain1 = forHwnd
                ? dxgiFactory2.CreateSwapChainForHwnd(dxgiDevice3, _win32Window.Handle, swapChainDescription1)
                : dxgiFactory2.CreateSwapChainForComposition(dxgiDevice3, swapChainDescription1);

            _swapChain = swapChain1.QueryInterface<IDXGISwapChain2>();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
        #endregion

        #region //Create render target view, get back buffer texture before
        // Get the first buffer of the swap chain as a texture.
        _renderTargetTexture = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        // Create a render target view for the render target texture.
        _renderTargetView = Device.CreateRenderTargetView(_renderTargetTexture);
        #endregion

        #region //Create depth stencil view
        // Create a depth stencil texture description with the specified properties.
        _depthStencilTextureDescription = new()
        {
            Format = Format.D32_Float, // Set format to D32_Float.
            ArraySize = 1,
            MipLevels = 0,
            Width = Size.Width,
            Height = Size.Height,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };

        // Create the depth stencil texture and view based on the description.
        using (_depthStencilTexture = Device.CreateTexture2D(_depthStencilTextureDescription))
            _depthStencilView = Device.CreateDepthStencilView(_depthStencilTexture);

        // Set up depth stencil description.
        DepthStencilDescription desc = new()
        {
            DepthEnable = true,
            DepthFunc = ComparisonFunction.Less,
            DepthWriteMask = DepthWriteMask.All,
        };

        // Create a depth stencil state from the description.
        ID3D11DepthStencilState state = Device.CreateDepthStencilState(desc);

        // Set the device context's OM state to the created depth stencil state.
        DeviceContext.OMSetDepthStencilState(state);
        // Set the device context's render targets to the created render target view and depth stencil view.
        DeviceContext.OMSetRenderTargets(_renderTargetView, _depthStencilView);
        #endregion

        #region //Create rasterizer state
        // Create a rasterizer state to fill the triangle using solid fill mode.
        RasterizerDescription rasterizerDesc = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
        };

        // Create a rasterizer state based on the description
        // and set it as the current state in the device context.
        DeviceContext.RSSetState(Device.CreateRasterizerState(rasterizerDesc));
        #endregion

        #region //Create Blend State
        // Set up the blend state description.
        BlendDescription blendStateDesc = new();

        // Render target blend description setup.
        RenderTargetBlendDescription renTarDesc = new()
        {
            BlendEnable = true, // Enable blend.
            SourceBlend = Blend.SourceAlpha,
            DestinationBlend = Blend.InverseSourceAlpha,
            BlendOperation = BlendOperation.Add,
            SourceBlendAlpha = Blend.One,
            DestinationBlendAlpha = Blend.Zero,
            BlendOperationAlpha = BlendOperation.Add,
            RenderTargetWriteMask = ColorWriteEnable.All
        };

        // Assign the render target blend description to the blend state description.
        blendStateDesc.RenderTarget[0] = renTarDesc;
        // Create the blend state.
        _blendState = Device.CreateBlendState(blendStateDesc);
        // Set the blend state.
        DeviceContext.OMSetBlendState(_blendState);
        #endregion

        #region //Set ViewPort
        // Set the viewport to match the size of the swap chain panel.
        DeviceContext.RSSetViewport(
            0, 0,
            Size.Width,
            Size.Height);
        #endregion

        return new Result(0);
    }

    public void Present() =>
        // Present the final render to the screen.
        _swapChain.Present(0, PresentFlags.None);

    public void Draw(ID3D11Buffer vertexBuffer, int vertexStride, ID3D11Buffer indexBuffer, int indexCount, int vertexOffset = 0, int indexOffset = 0)
    {
        // Set the type of the primitive to be rendered in triangle list.
        DeviceContext.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);
        // Set the vertex buffer.
        DeviceContext.IASetVertexBuffer(0, vertexBuffer, vertexStride, vertexOffset);
        // Set the index buffer in R16_UInt format.
        DeviceContext.IASetIndexBuffer(indexBuffer, Format.R16_UInt, indexOffset);
        // Set the blend state.
        DeviceContext.OMSetBlendState(_blendState);

        // Make the draw call to render the geometry.
        DeviceContext.DrawIndexed(indexCount, 0, 0);
    }

    public void Clear()
    {
        // Set the background color to a dark gray.
        var col = new Color4(0.15f, 0.15f, 0.15f, 1);

        // Clear the render target view and depth stencil view with the set color.
        DeviceContext.ClearRenderTargetView(_renderTargetView, col);
        DeviceContext.ClearDepthStencilView(_depthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

        // Set the render target and depth stencil view for the device context.
        DeviceContext.OMSetRenderTargets(_renderTargetView, _depthStencilView);

        // Reset the profiler values for vertices, indices, and draw calls.
        Profiler.Vertices = 0;
        Profiler.Indices = 0;
        Profiler.DrawCalls = 0;
    }

    public void SetRasterizerDesc(FillMode fillMode = FillMode.Solid)
    {
        // Create a rasterizer state with specified fill and cull modes.
        RasterizerDescription rasterizerDesc = new()
        {
            FillMode = fillMode,
            CullMode = fillMode == FillMode.Solid ? CullMode.Back : CullMode.None,
            FrontCounterClockwise = true
        };

        // Create a rasterizer state based on the description
        // and set it as the current state in the device context.
        DeviceContext.RSSetState(Device.CreateRasterizerState(rasterizerDesc));
    }

    public void Dispose()
    {
        // Dispose all DirectX resources that were created.
        Device?.Dispose();
        DeviceContext?.Dispose();

        // Dispose of the swap chain.
        _swapChain?.Dispose();

        // Dispose of the render target texture and view.
        _renderTargetTexture?.Dispose();
        _renderTargetView?.Dispose();

        // Dispose of the depth stencil texture and view.
        _depthStencilTexture?.Dispose();
        _depthStencilView?.Dispose();

        // Dispose of the blend state.
        _blendState?.Dispose();

    }

    public void OnSwapChainSizeChanged(int newWidth, int newHeight)
    {
        if (!IsRendering)
            return;

        // Resize the buffers, depth stencil texture, render target texture and viewport
        // when the size of the window changes.
        Size = new Size(
            Math.Max(640, newWidth),
            Math.Max(480, newHeight));

        // Dispose the existing render target view, render target texture, depth stencil view, and depth stencil texture.
        _renderTargetView?.Dispose();
        _renderTargetTexture?.Dispose();
        _depthStencilView?.Dispose();
        _depthStencilTexture?.Dispose();

        // Resize the swap chain buffers to match the new window size.
        _swapChain.ResizeBuffers(
            _swapChain.Description.BufferCount,
            Size.Width,
            Size.Height,
            _swapChain.Description1.Format,
            _swapChain.Description1.Flags);

        // Get the render target texture and create the render target view.
        _renderTargetTexture = _swapChain.GetBuffer<ID3D11Texture2D>(0);
        _renderTargetView = Device.CreateRenderTargetView(_renderTargetTexture);

        // Update the depth stencil texture description and create the depth stencil texture and view.
        _depthStencilTextureDescription.Width = Size.Width;
        _depthStencilTextureDescription.Height = Size.Height;
        using (_depthStencilTexture = Device.CreateTexture2D(_depthStencilTextureDescription))
            _depthStencilView = Device.CreateDepthStencilView(_depthStencilTexture);

        // Update the size of the source in the swap chain.
        _swapChain.SourceSize = Size;

        // Update the viewport to match the new window size.
        DeviceContext.RSSetViewport(
            0, 0,
            Size.Width,
            Size.Height);
    }
}
