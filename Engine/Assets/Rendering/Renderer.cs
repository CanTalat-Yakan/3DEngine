using System.Drawing;

using SharpGen.Runtime;
using Vortice.Direct3D12.Debug;
using Vortice.Direct3D12;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Rendering;

public sealed partial class Renderer
{
    public static Renderer Instance { get; private set; }

    public bool IsRendering => Data.BufferRenderTargetView?.NativePointer is not 0;
    public IDXGISwapChain3 SwapChain => Data.SwapChain;

    public Size Size => NativeSize.Scale(Config.ResolutionScale);
    public Size NativeSize { get; private set; }

    public ID3D12Device2 Device { get; private set; }

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
        // Set the singleton instance of the class, if it hasn't been already.
        Instance ??= this;

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
        CreateDevice(out var result);
        if (result.Failure)
            return result;

        CreateGraphicsQueueAndFence(Device);

        SetupSwapChain(forHwnd);

        GetSwapChainBuffersAndCreateRenderTargetViews();
        CreateMSAATextureAndRenderTargetView();
        CreateDepthStencilView();

        CreateBlendDescription();
        CreateRasterizerDescription();

        return Result.Ok;
    }

    public void Dispose()
    {
        WaitIdle();

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

        GetSwapChainBuffersAndCreateRenderTargetViews();
        CreateMSAATextureAndRenderTargetView();
        CreateDepthStencilView();

        // Update the size of the source in the swap chain.
        Data.SwapChain.SourceSize = Size;

        Core.Instance.Frame();
    }
}

public sealed partial class Renderer
{
    public void Present() =>
        // Present the final render to the screen.
        Data.SwapChain.Present((int)Config.VSync, PresentFlags.DoNotWait);

    public void Resolve() =>
        // Copy the MSAA render target texture into the back buffer render texture.
        Data.Material.CommandList.ResolveSubresource(Data.BackBufferRenderTargetTexture, 0, Data.MSAARenderTargetTexture, 0, RenderData.RenderTargetFormat);
    // Use this to Copy: .CopyResource(dstResource, srcResource);

    public void EndFrame()
    {
        // Indicate that the back buffer will now be used to present.
        Data.Material?.CommandList.ResourceBarrierTransition(Data.MSAARenderTargetTexture, ResourceStates.RenderTarget, ResourceStates.AllShaderResource);
        Data.Material?.CommandList.EndEvent();

        Data.GraphicsQueue.Signal(Data.FrameFence, ++Data.FrameCount);

        ulong GPUFrameCount = Data.FrameFence.CompletedValue;

        if ((Data.FrameCount - GPUFrameCount) >= RenderData.RenderLatency)
        {
            Data.FrameFence.SetEventOnCompletion(GPUFrameCount + 1, Data.FrameFenceEvent);
            Data.FrameFenceEvent.WaitOne();
        }

        WaitIdle();
    }

    public void EndRenderPass() =>
        Data.Material.CommandList.EndRenderPass();

    public void Draw(int indexCount, IndexBufferView indexBufferViews, params VertexBufferView[] vertexBufferViews)
    {
        // Indicate that the MSAA render target texture will be used as a render target.
        Data.Material.CommandList.ResourceBarrierTransition(Data.MSAARenderTargetTexture, ResourceStates.AllShaderResource, ResourceStates.RenderTarget);

        Data.Material.CommandList.BeginEvent("Frame");

        Data.SetupInputAssembler(indexBufferViews, vertexBufferViews);

        Data.Material.CommandList.DrawIndexedInstanced(indexCount, 1, 0, 0, 0);
    }

    public void BeginFrame(bool useRenderPass = false)
    {

        // Set the background color to a dark gray.
        var clearColor = Colors.DarkGray;

        if (useRenderPass)
        {
            var renderPassDesc = new RenderPassRenderTargetDescription(Data.BufferRenderTargetView.GetCPUDescriptorHandleForHeapStart(),
                new RenderPassBeginningAccess(new ClearValue(RenderData.RenderTargetFormat, clearColor)),
                new RenderPassEndingAccess(RenderPassEndingAccessType.Preserve));

            var depthStencil = new RenderPassDepthStencilDescription(
                Data.DepthStencilView.GetCPUDescriptorHandleForHeapStart(),
                new RenderPassBeginningAccess(new ClearValue(RenderData.DepthStencilFormat, 1.0f, 0)),
                new RenderPassEndingAccess(RenderPassEndingAccessType.Discard));

            Data.Material?.CommandList.BeginRenderPass(renderPassDesc, depthStencil);
        }
        else
        {
            // Clear the render target view and depth stencil view with the set color.
            Data.Material?.CommandList.ClearRenderTargetView(Data.MSAARenderTargetView.GetCPUDescriptorHandleForHeapStart(), clearColor);
            Data.Material?.CommandList.ClearDepthStencilView(Data.DepthStencilView.GetCPUDescriptorHandleForHeapStart(), ClearFlags.Depth | ClearFlags.Stencil, 1.0f, 0);

            // Set the render target and depth stencil view for the device context.
            Data.Material?.CommandList.OMSetRenderTargets(Data.MSAARenderTargetView.GetCPUDescriptorHandleForHeapStart(), Data.DepthStencilView.GetCPUDescriptorHandleForHeapStart());
        }

        // Reset the profiler values for vertices, indices, and draw calls.
        Profiler.Vertices = 0;
        Profiler.Indices = 0;
        Profiler.DrawCalls = 0;
    }

    public void WaitIdle()
    {
        Data.GraphicsQueue.Signal(Data.FrameFence, ++Data.FrameCount);
        Data.FrameFence.SetEventOnCompletion(Data.FrameCount, Data.FrameFenceEvent);
        Data.FrameFenceEvent.WaitOne();
    }
}

public sealed partial class Renderer
{
    private static readonly FeatureLevel[] s_featureLevels = new[]
    {
        FeatureLevel.Level_12_2,
        FeatureLevel.Level_12_1,
        FeatureLevel.Level_12_0,
        FeatureLevel.Level_11_1,
        FeatureLevel.Level_11_0,
    };

    private void CreateDevice(out Result result)
    {
        if (Config.Debug
            && D3D12.D3D12GetDebugInterface(out ID3D12Debug debug).Success)
        {
            debug!.EnableDebugLayer();
            debug!.Dispose();
        }
        else
            Config.Debug = false;

        using (IDXGIFactory4 DXGIFactory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(Config.Debug))
        {
            ID3D12Device2 d3d12Device = default;
            IDXGIAdapter1 adapter = default;

            for (int i = 0; DXGIFactory.EnumAdapters1(i, out adapter).Success; i++)
                // Don't select the Basic Render Driver adapter.
                if ((adapter.Description1.Flags & AdapterFlags.Software) is not AdapterFlags.None)
                    adapter.Dispose();
                else break;

            for (int i = 0; i < s_featureLevels.Length; i++)
                // Create the D3D12 Device with the current adapter and the highest possible Feature level.
                if (D3D12.D3D12CreateDevice(adapter, s_featureLevels[i], out d3d12Device).Success)
                {
                    adapter.Dispose();
                    break;
                }

            if (d3d12Device is null)
            {
                result = Result.Fail;
                return;
            }
            else
            {
                // Assign the device to a variable.
                Device = d3d12Device!;
                Device.Name = "Device";
            }
        }

        result = Result.Ok;
    }

    private void CreateGraphicsQueueAndFence(ID3D12Device2 device)
    {
        // Create Command queue.
        Data.GraphicsQueue = device.CreateCommandQueue(CommandListType.Direct);
        Data.GraphicsQueue.Name = "Graphics Queue";

        // Create synchronization objects.
        Data.FrameFence = Device.CreateFence(0);
        Data.FrameFenceEvent = new System.Threading.AutoResetEvent(false);
    }

    private void SetupSwapChain(bool forHwnd)
    {
        // Initialize the SwapChainDescription structure.
        SwapChainDescription1 swapChainDescription = new()
        {
            BufferCount = RenderData.RenderLatency,
            Width = Size.Width,
            Height = Size.Height,
            Format = RenderData.RenderTargetFormat,
            BufferUsage = Usage.RenderTargetOutput,
            SwapEffect = SwapEffect.FlipSequential,
            SampleDescription = SampleDescription.Default
        };

        // Create the IDXGIFactory4.
        using IDXGIFactory4 DXGIFactory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(Config.Debug);
        // Creates a swap chain using the swap chain description.
        using IDXGISwapChain1 swapChain1 = forHwnd
            ? DXGIFactory.CreateSwapChainForHwnd(Data.GraphicsQueue, _win32Window.Handle, swapChainDescription)
            : DXGIFactory.CreateSwapChainForComposition(Data.GraphicsQueue, swapChainDescription);

        Data.SwapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
    }

    private void GetSwapChainBuffersAndCreateRenderTargetViews()
    {
        Data.BufferRenderTargetTextures = new ID3D12Resource[RenderData.RenderLatency];
        Data.BufferRenderTargetView = Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = RenderData.RenderLatency,
            Type = DescriptorHeapType.RenderTargetView
        });
        for (int i = 0; i < RenderData.RenderLatency; i++)
        {
            // Get the buffers of the swap chain as a texture.
            Data.BufferRenderTargetTextures[i] = Data.SwapChain.GetBuffer<ID3D12Resource>(i);
            Data.BufferRenderTargetTextures[i].Name = $"SpawChain Buffer {i}";
            // Create a render target view for the back buffer render target texture.
            Device.CreateRenderTargetView(Data.BufferRenderTargetTextures[i], null, Data.BufferRenderTargetView.GetCPUDescriptorHandleForHeapStart());
        }

        var size = Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        Data.BufferRenderTargetView.GetCPUDescriptorHandleForHeapStart().Offset(size, RenderData.RenderLatency);
    }

    private void CheckMSAASupport()
    {
        int qualityLevels = 0;
        int sampleCount;
        for (sampleCount = (int)Config.MultiSample; sampleCount > 1; sampleCount /= 2)
        {
            qualityLevels = Device.CheckMultisampleQualityLevels(RenderData.RenderTargetFormat, sampleCount);
            if (qualityLevels > 0)
                break;
        }

        if (sampleCount < 2 && Config.MultiSample != MultiSample.None)
            Output.Log("MSAA not supported");

        Config.SupportedSampleCount = sampleCount;
        Config.QualityLevels = qualityLevels - 1;
    }

    private void CreateMSAATextureAndRenderTargetView()
    {
        CheckMSAASupport();

        ResourceDescription MSAATextureDescription = ResourceDescription.Texture2D(
            RenderData.RenderTargetFormat,
            (uint)Size.Width,
            (uint)Size.Height,
            arraySize: 1,
            mipLevels: 1,
            sampleCount: Config.SupportedSampleCount,
            sampleQuality: Config.QualityLevels,
            flags: ResourceFlags.AllowRenderTarget); // BindFlags.ShaderResource

        // Create the multi sample texture based on the description.
        Data.MSAARenderTargetTexture = Device.CreateCommittedResource(
            HeapType.Default,
            MSAATextureDescription,
            ResourceStates.AllShaderResource);
        Data.MSAARenderTargetTexture.Name = "MSAA Render Target Texture";

        // Create a render target view description for the multi sampling.
        RenderTargetViewDescription MSAARenderTargetViewDescription = new()
        {
            Format = RenderData.RenderTargetFormat,
            ViewDimension = RenderTargetViewDimension.Texture2DMultisampled
        };

        Data.MSAARenderTargetView = Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Type = DescriptorHeapType.RenderTargetView
        });
        // Create a render target view for the MSAA render target texture.
        Device.CreateRenderTargetView(Data.MSAARenderTargetTexture, MSAARenderTargetViewDescription, Data.MSAARenderTargetView.GetCPUDescriptorHandleForHeapStart());

        var size = Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.RenderTargetView);
        Data.MSAARenderTargetView.GetCPUDescriptorHandleForHeapStart().Offset(size);
    }

    private void CreateDepthStencilView()
    {
        ResourceDescription depthStencilTextureDescription = ResourceDescription.Texture2D(
            RenderData.DepthStencilFormat,
            (uint)Size.Width,
            (uint)Size.Height,
            arraySize: 1,
            mipLevels: 1,
            sampleCount: Config.SupportedSampleCount,
            sampleQuality: Config.QualityLevels,
            flags: ResourceFlags.AllowDepthStencil);

        // Create the depth stencil texture based on the description.
        Data.DepthStencilTexture = Device.CreateCommittedResource(
            HeapType.Default,
            depthStencilTextureDescription,
            ResourceStates.DepthWrite,
            new(RenderData.DepthStencilFormat, 1.0f, 0));
        Data.DepthStencilTexture.Name = "DepthStencil Texture";

        // Create a depth stencil view description for the multi sampling.
        DepthStencilViewDescription depthStencilViewDescription = new()
        {
            Format = RenderData.DepthStencilFormat,
            ViewDimension = DepthStencilViewDimension.Texture2DMultisampled
        };

        Data.DepthStencilView = Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Type = DescriptorHeapType.DepthStencilView
        });
        // Create a depth stencil view for the depth stencil texture.
        Device.CreateDepthStencilView(Data.DepthStencilTexture, depthStencilViewDescription, Data.DepthStencilView.GetCPUDescriptorHandleForHeapStart());

        var size = Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.DepthStencilView);
        Data.DepthStencilView.GetCPUDescriptorHandleForHeapStart().Offset(size);
    }

    private void CreateBlendDescription()
    {
        // Set up the blend state description.
        Data.BlendDescription = new()
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
        Data.BlendDescription.RenderTarget[0] = renderTargetBlendDescription;
    }

    private void CreateRasterizerDescription()
    {
        // Create a rasterizer description
        // using solid fill mode, multi sampled and counter clockwise.
        Data.RasterizerDescription = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            AntialiasedLineEnable = true,
            MultisampleEnable = true,
            FrontCounterClockwise = true
        };
    }
}