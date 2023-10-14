using SharpGen.Runtime;
using System.Drawing;

using Vortice.Direct3D11;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Rendering;

public sealed partial class Renderer
{
    public static Renderer Instance { get; private set; }

    public bool IsRendering => Data.BackBufferRenderTargetView?.NativePointer is not 0;
    public IDXGISwapChain2 SwapChain => Data.SwapChain;

    public Size Size => NativeSize.Scale(Config.ResolutionScale);
    public Size NativeSize { get; private set; }

    public ID3D11Device Device { get; private set; }

    public RenderData Data = new();
    public Config Config = new();

    private Win32Window _win32Window;

    public Renderer(Win32Window win32Window, Config config = null)
    {
        // Initializes the singleton instance of the class, if it hasn't been already.
        if (Instance is null)
            Instance = this;

        // Store the instance of Win32Window.
        if (win32Window is not null)
            _win32Window = win32Window;
        else
            throw new Exception(
                """
                An invalid or null Win32Window instance was passed to the Renderer. 
                Please ensure that you provide a valid Win32Window object to the Renderer.
                """);

        if (config is not null)
            Config = config;

        // Set the size.
        NativeSize = new Size(
            _win32Window.Width,
            _win32Window.Height);

        var result = Initialize(true);
        if (result.Failure)
            throw new Exception(result.Description);
    }

    public Renderer(int sizeX = 640, int sizeY = 480, Config config = null)
    {
        if (Instance is null)
            Instance = this;

        if (config is not null)
            Config = config;

        NativeSize = new Size(
            Math.Max(64, sizeX),
            Math.Max(64, sizeY));

        var result = Initialize();
        if (result.Failure)
            throw new Exception(result.Description);
    }

    private Result Initialize(bool forHwnd = false)
    {
        CreateDeviceAndSetupSwapChain(forHwnd, out Result result);
        if (result.Failure)
            return result;

        GetBackBufferAndCreateRenderTargetView();
        CreateMSAATextureAndRenderTargetView();
        CreateDepthStencilView();
        CreateBlendState();
        CreateRasterizerState();

        return Result.Ok;
    }
}

public sealed partial class Renderer
{
    public void Present() =>
        // Present the final render to the screen.
        Data.SwapChain.Present((int)Config.VSync, PresentFlags.DoNotWait);

    public void Resolve() =>
        // Copy the MSAA render target texture into the back buffer render texture.
        Data.DeviceContext.ResolveSubresource(Data.BackBufferRenderTargetTexture, 0, Data.BackBufferRenderTargetTexture, 0, Data.Format);
    //Data.DeviceContext.ResolveSubresource(Data.BackBufferRenderTargetTexture, 0, Data.MSAARenderTargetTexture, 0, Data.Format);

    public void Draw(ID3D11Buffer vertexBuffer, ID3D11Buffer indexBuffer, int indexCount)
    {
        Data.VertexBuffer = vertexBuffer;
        Data.IndexBuffer = indexBuffer;

        Data.RasterizerState = Device.CreateRasterizerState(Data.RasterizerDescription);

        Data.SetupRenderState();

        Data.DeviceContext.DrawIndexed(indexCount, 0, 0);
    }

    public void DrawIndexed(int indexCount, int startIndexLocation = 0, int baseVertexLocation = 0) =>
        Data.DeviceContext.DrawIndexed(indexCount, startIndexLocation, baseVertexLocation);

    public void Clear()
    {
        // Set the background color to a dark gray.
        var col = new Color4(0.15f, 0.15f, 0.15f, 0);

        // Clear the render target view and depth stencil view with the set color.
        Data.DeviceContext.ClearRenderTargetView(Data.BackBufferRenderTargetView, col);
        Data.DeviceContext.ClearDepthStencilView(Data.DepthStencilView, DepthStencilClearFlags.Depth | DepthStencilClearFlags.Stencil, 1.0f, 0);

        // Set the render target and depth stencil view for the device context.
        //Data.DeviceContext.OMSetRenderTargets(Data.MSAARenderTargetView, Data.DepthStencilView);
        Data.DeviceContext.OMSetRenderTargets(Data.BackBufferRenderTargetView, Data.DepthStencilView);

        // Reset the profiler values for vertices, indices, and draw calls.
        Profiler.Vertices = 0;
        Profiler.Indices = 0;
        Profiler.DrawCalls = 0;
    }

    public void Dispose()
    {
        // Dispose all DirectX resources that were created.
        Device?.Dispose();
        Data.Dispose();
    }

    public void Resize(int newWidth, int newHeight)
    {
        if (!IsRendering)
            return;

        // Resize the buffers, depth stencil texture, render target texture and viewport
        // when the size of the window changes.
        NativeSize = new Size(
            Math.Max(64, newWidth),
            Math.Max(64, newHeight));

        // Dispose the existing render target views/ textures and depth stencil view/ texture.
        Data.DisposeTexturesAndViews();

        // Resize the swap chain buffers to match the new window size.
        Data.SwapChain.ResizeBuffers(
            Data.SwapChain.Description.BufferCount,
            Size.Width,
            Size.Height,
            Data.SwapChain.Description1.Format,
            Data.SwapChain.Description1.Flags);

        GetBackBufferAndCreateRenderTargetView();
        CreateMSAATextureAndRenderTargetView();
        CreateDepthStencilView();

        // Update the size of the source in the swap chain.
        Data.SwapChain.SourceSize = Size;

        Core.Instance.Frame();
    }
}

public sealed partial class Renderer
{
    private Result CreateDeviceAndSetupSwapChain(bool forHwnd, out Result result)
    {
        // Create a Direct3D 11 device.
        result = D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.None,
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
        Device = defaultDevice.QueryInterface<ID3D11Device>();

        Data.DeviceContext = Device.ImmediateContext;

        // Initialize the SwapChainDescription structure.
        SwapChainDescription1 swapChainDescription = new()
        {
            AlphaMode = AlphaMode.Ignore,
            BufferCount = 2,
            Format = Data.Format = Format.R8G8B8A8_UNorm, // 10 bits = Format.R10G10B10A2_UNorm
            Width = Size.Width,
            Height = Size.Height,
            SampleDescription = new(1, 0),
            Scaling = Scaling.Stretch,
            Stereo = false,
            SwapEffect = SwapEffect.FlipSequential,
            BufferUsage = Usage.RenderTargetOutput,
            Flags = SwapChainFlags.None
        };

        try
        {
            // Obtain instance of the IDXGIDevice3 interface from the Direct3D device.
            IDXGIDevice3 dxgiDevice3 = Device.QueryInterface<IDXGIDevice3>();
            // Obtain instance of the IDXGIFactory2 interface from the DXGI device.
            IDXGIFactory2 dxgiFactory2 = dxgiDevice3.GetAdapter().GetParent<IDXGIFactory2>();
            // Creates a swap chain using the swap chain description.
            IDXGISwapChain1 swapChain1 = forHwnd
                ? dxgiFactory2.CreateSwapChainForHwnd(dxgiDevice3, _win32Window.Handle, swapChainDescription)
                : dxgiFactory2.CreateSwapChainForComposition(dxgiDevice3, swapChainDescription);

            Data.SwapChain = swapChain1.QueryInterface<IDXGISwapChain2>();
        }
        catch (Exception ex) { throw new Exception(ex.Message); }

        return result;
    }

    private void GetBackBufferAndCreateRenderTargetView()
    {
        // Get the back buffer of the swap chain as a texture.
        Data.BackBufferRenderTargetTexture = Data.SwapChain.GetBuffer<ID3D11Texture2D>(0);
        // Create a render target view for the back buffer render target texture.
        Data.BackBufferRenderTargetView = Device.CreateRenderTargetView(Data.BackBufferRenderTargetTexture);
    }

    private void CheckMSAASupport()
    {
        Result result;
        // Note that 4x MSAA is required for Direct3D Feature Level 10.1 or better.
        //           8x MSAA is required for Direct3D Feature Level 11.0 or better.
        int sampleCount;
        for (sampleCount = (int)Config.MultiSample; sampleCount > 1; sampleCount--)
        {
            result = Device.CheckMultisampleQualityLevels(Data.Format, sampleCount);
            if (result.Success)
                break;
        }

        if (sampleCount < 2)
            throw new Exception("MSAA not supported");

        Config.SupportedSampleCount = sampleCount;
    }

    private void CreateMSAATextureAndRenderTargetView()
    {
        CheckMSAASupport();

        Texture2DDescription MSAATextureDescription = new()
        {
            Format = Data.Format,
            Width = Size.Width,
            Height = Size.Height,
            ArraySize = 1,
            MipLevels = 1,
            SampleDescription = new(Config.SupportedSampleCount, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.RenderTarget | BindFlags.ShaderResource,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };
        // Create a multi sample texture .
        Data.MSAARenderTargetTexture = Device.CreateTexture2D(MSAATextureDescription);

        // Create a render target view description for the multi sampling.
        RenderTargetViewDescription MSAARenderTargetViewDescription = new(RenderTargetViewDimension.Texture2DMultisampled, Data.Format);
        // Create a render target view for the MSAA render target texture.
        Data.MSAARenderTargetView = Device.CreateRenderTargetView(Data.MSAARenderTargetTexture, MSAARenderTargetViewDescription);
    }

    private void CreateDepthStencilView()
    {
        // Set up depth stencil description.
        DepthStencilDescription depthStencilDescription = new()
        {
            DepthEnable = true,
            DepthFunc = ComparisonFunction.Less,
            DepthWriteMask = DepthWriteMask.All,
        };
        // Create a depth stencil state from the description.
        Data.DepthStencilState = Device.CreateDepthStencilState(depthStencilDescription);

        // Create a depth stencil texture description with the specified properties.
        Texture2DDescription depthStencilTextureDescription = new()
        {
            Format = Format.D32_Float, // Set format to D32_Float.
            Width = Size.Width,
            Height = Size.Height,
            ArraySize = 1,
            MipLevels = 0,
            SampleDescription = new(1, 0),
            Usage = ResourceUsage.Default,
            BindFlags = BindFlags.DepthStencil,
            CPUAccessFlags = CpuAccessFlags.None,
            MiscFlags = ResourceOptionFlags.None
        };
        // Create the depth stencil texture and view based on the description.
        Data.DepthStencilTexture = Device.CreateTexture2D(depthStencilTextureDescription);

        // Create a depth stencil view description for the multi sampling.
        DepthStencilViewDescription depthStencilViewDescription = new(DepthStencilViewDimension.Texture2DMultisampled, Format.D32_Float);
        Data.DepthStencilView = Device.CreateDepthStencilView(Data.DepthStencilTexture, depthStencilViewDescription);
    }

    private void CreateBlendState()
    {
        // Set up the blend state description.
        BlendDescription blendDescription = new()
        {
            AlphaToCoverageEnable = true
        };

        // Render target blend description setup.
        RenderTargetBlendDescription renderTargetBlendDescription = new()
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
        blendDescription.RenderTarget[0] = renderTargetBlendDescription;
        // Create the blend state.
        Data.BlendState = Device.CreateBlendState(blendDescription);
    }

    private void CreateRasterizerState()
    {
        // Create a rasterizer state to fill the triangle using solid fill mode.
        Data.RasterizerDescription = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            AntialiasedLineEnable = true,
            MultisampleEnable = true
        };

        // Create a rasterizer state based on the description.
        Data.RasterizerState = Device.CreateRasterizerState(Data.RasterizerDescription);
    }
}
