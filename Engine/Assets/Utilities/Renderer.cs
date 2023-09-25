using SharpGen.Runtime;
using System.Drawing;

using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Utilities;

public sealed class Renderer
{
    public static Renderer Instance { get; private set; }

    public bool IsRendering => Data.RenderTargetView.NativePointer is not 0;
    public IDXGISwapChain2 SwapChain => Data.SwapChain;

    public Size Size => Data.SuperSample ? NativeSize * 2 : NativeSize;
    public Size NativeSize { get; private set; }

    public ID3D11Device Device { get; private set; }

    public RenderData Data = new();

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
                Please ensure that you provide a valid Win32Window object to the Renderer.
                """);

        // Set the size.
        NativeSize = new Size(
            _win32Window.Width,
            _win32Window.Height);

        var result = Initialize(true);
        if (result.Failure)
            throw new Exception(result.Description);
    }

    public Renderer(int sizeX = 640, int sizeY = 480)
    {
        if (Instance is null)
            Instance = this;

        NativeSize = new Size(
            Math.Max(640, sizeX),
            Math.Max(480, sizeY));

        var result = Initialize();
        if (result.Failure)
            throw new Exception(result.Description);
    }

    public Result Initialize(bool forHwnd = false)
    {
        #region //Create device, device context & swap chain with result
        // Create a Direct3D 11 device.
        var result = D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
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
        Device = defaultDevice.QueryInterface<ID3D11Device>(); // Due to unsupported SDK (Idk), switched from ID3D11Device1 to ID3D11Device Interface, Context too.
                                                               // Get the immediate context of the device.
        Data.DeviceContext = Device.ImmediateContext;

        // Initialize the SwapChainDescription structure.
        Data.SwapChainDescription = new()
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
            IDXGIFactory2 dxgiFactory2 = dxgiDevice3.GetAdapter().GetParent<IDXGIFactory2>();
            // Creates a swap chain using the swap chain description.
            IDXGISwapChain1 swapChain1 = forHwnd
                ? dxgiFactory2.CreateSwapChainForHwnd(dxgiDevice3, _win32Window.Handle, Data.SwapChainDescription)
                : dxgiFactory2.CreateSwapChainForComposition(dxgiDevice3, Data.SwapChainDescription);

            Data.SwapChain = swapChain1.QueryInterface<IDXGISwapChain2>();
        }
        catch (Exception e)
        {
            throw new Exception(e.Message);
        }
        #endregion

        #region //Create render target view, get back buffer texture before
        // Get the first buffer of the swap chain as a texture.
        Data.RenderTargetTexture = Data.SwapChain.GetBuffer<ID3D11Texture2D>(0);
        // Create a render target view for the render target texture.
        Data.RenderTargetView = Device.CreateRenderTargetView(Data.RenderTargetTexture);
        #endregion

        #region //Create Blend State
        // Set up the blend state description.
        Data.BlendStateDescription = new()
        {
            AlphaToCoverageEnable = false
        };

        // Render target blend description setup.
        Data.RenderTargetBlendDescription = new()
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
        Data.BlendStateDescription.RenderTarget[0] = Data.RenderTargetBlendDescription;
        // Create the blend state.
        Data.BlendState = Device.CreateBlendState(Data.BlendStateDescription);
        #endregion

        #region //Create depth stencil view
        // Set up depth stencil description.
        Data.DepthStencilDescription = new()
        {
            DepthEnable = true,
            DepthFunc = ComparisonFunction.Less,
            DepthWriteMask = DepthWriteMask.All,
        };

        // Create a depth stencil state from the description.
        Data.DepthStencilState = Device.CreateDepthStencilState(Data.DepthStencilDescription);

        // Create a depth stencil texture description with the specified properties.
        Data.DepthStencilTextureDescription = new()
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
        Data.DepthStencilTexture = Device.CreateTexture2D(Data.DepthStencilTextureDescription);
        Data.DepthStencilView = Device.CreateDepthStencilView(Data.DepthStencilTexture);

        // Set the device context's render targets to the created render target view and depth stencil view.
        Data.DeviceContext.OMSetRenderTargets(Data.RenderTargetView, Data.DepthStencilView);
        #endregion

        #region //Create rasterizer state
        // Create a rasterizer state to fill the triangle using solid fill mode.
        Data.RasterizerDescription = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
        };

        // Create a rasterizer state based on the description
        Data.RasterizerState = Device.CreateRasterizerState(Data.RasterizerDescription);
        #endregion

        return new Result(0);
    }

    public void Present() =>
        // Present the final render to the screen.
        Data.SwapChain.Present(Data.VSync ? 1 : 0, PresentFlags.None);

    public void Draw(ID3D11Buffer vertexBuffer, ID3D11Buffer indexBuffer, int indexCount, int vertexStride, int vertexOffset = 0, int indexOffset = 0)
    {
        Data.VertexBuffer = vertexBuffer;
        Data.IndexBuffer = indexBuffer;

        Data.RasterizerState = Device.CreateRasterizerState(Data.RasterizerDescription);

        Data.SetupRenderState(vertexStride, vertexOffset);
        Data.SetViewport(Size.Width, Size.Height);

        Data.DeviceContext.DrawIndexed(indexCount, 0, 0);
    }

    public void Clear()
    {
        // Set the background color to a dark gray.
        var col = new Color4(0.15f, 0.15f, 0.15f, 0);

        // Clear the render target view and depth stencil view with the set color.
        Data.DeviceContext.ClearRenderTargetView(Data.RenderTargetView, col);
        Data.DeviceContext.ClearDepthStencilView(Data.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);

        // Set the render target and depth stencil view for the device context.
        Data.DeviceContext.OMSetRenderTargets(Data.RenderTargetView, Data.DepthStencilView);

        // Reset the profiler values for vertices, indices, and draw calls.
        Profiler.Vertices = 0;
        Profiler.Indices = 0;
        Profiler.DrawCalls = 0;
    }

    public void Dispose()
    {
        // Dispose all DirectX resources that were created.
        Device?.Dispose();
        Data.DeviceContext?.Dispose();

        // Dispose of the swap chain.
        Data.SwapChain?.Dispose();

        // Dispose of the render target texture and view.
        Data.RenderTargetTexture?.Dispose();
        Data.RenderTargetView?.Dispose();

        // Dispose of the depth stencil texture and view.
        Data.DepthStencilTexture?.Dispose();
        Data.DepthStencilView?.Dispose();

        // Dispose of the blend state.
        Data.BlendState?.Dispose();

    }

    public void Resize(int newWidth, int newHeight)
    {
        if (!IsRendering)
            return;

        // Resize the buffers, depth stencil texture, render target texture and viewport
        // when the size of the window changes.
        NativeSize = new Size(
            Math.Max(640, newWidth),
            Math.Max(480, newHeight));

        // Dispose the existing render target view, render target texture, depth stencil view, and depth stencil texture.
        Data.RenderTargetView?.Dispose();
        Data.RenderTargetTexture?.Dispose();
        Data.DepthStencilView?.Dispose();
        Data.DepthStencilTexture?.Dispose();

        // Resize the swap chain buffers to match the new window size.
        Data.SwapChain.ResizeBuffers(
            Data.SwapChain.Description.BufferCount,
            Size.Width,
            Size.Height,
            Data.SwapChain.Description1.Format,
            Data.SwapChain.Description1.Flags);

        // Get the render target texture and create the render target view.
        Data.RenderTargetTexture = Data.SwapChain.GetBuffer<ID3D11Texture2D>(0);
        Data.RenderTargetView = Device.CreateRenderTargetView(Data.RenderTargetTexture);

        // Update the depth stencil texture description and create the depth stencil texture and view.
        Data.DepthStencilTextureDescription.Width = Size.Width;
        Data.DepthStencilTextureDescription.Height = Size.Height;
        Data.DepthStencilTexture = Device.CreateTexture2D(Data.DepthStencilTextureDescription);
        Data.DepthStencilView = Device.CreateDepthStencilView(Data.DepthStencilTexture);

        // Update the size of the source in the swap chain.
        Data.SwapChain.SourceSize = Size;
    }
}
