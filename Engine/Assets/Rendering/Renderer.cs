using SharpGen.Runtime;
using System.Drawing;

using Vortice.Direct3D12;
using Vortice.Direct3D;
using Vortice.DXGI;
using Vortice.Mathematics;
using Engine.Data;

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

        GetSwapChainBuffersAndRenderTargetViews();
        CreateMSAATextureAndRenderTargetView();
        CreateDepthStencilView();
        CreateRasterizerState();

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

        GetSwapChainBuffersAndRenderTargetViews();
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
        // Use this to Copy: Data.DeviceContext.CopyResource(Data.BackBufferRenderTargetTexture, Data.MSAARenderTargetTexture);
        Data.Material.CommandList.ResolveSubresource(Data.BackBufferRenderTargetTexture, 0, Data.MSAARenderTargetTexture, 0, RenderData.RenderTargetFormat);

    public void EndFrame()
    {
        // Indicate that the back buffer will now be used to present.
        Data.Material.CommandList.ResourceBarrierTransition(Data.MSAARenderTargetTexture, ResourceStates.RenderTarget, ResourceStates.Present);
        Data.Material.CommandList.EndEvent();
        Data.Material.CommandList.Close();

        Data.GraphicsQueue.Signal(Data.FrameFence, ++Data.FrameCount);

        ulong GPUFrameCount = Data.FrameFence.CompletedValue;

        if ((Data.FrameCount - GPUFrameCount) >= RenderData.RenderLatency)
        {
            Data.FrameFence.SetEventOnCompletion(GPUFrameCount + 1, Data.FrameFenceEvent);
            Data.FrameFenceEvent.WaitOne();
        }
    }

    public void EndRenderPass() =>
        Data.Material.CommandList.EndRenderPass();

    public void Draw(int indexCount, IndexBufferView indexBufferViews, params VertexBufferView[] vertexBufferViews)
    {
        Data.SetupInputAssembler(indexBufferViews, vertexBufferViews);

        Data.Material.CommandList.DrawIndexedInstanced(indexCount, 1, 0, 0, 0);
    }

    public void BeginFrame(bool useRenderPass = false)
    {
        // Indicate that the MSAA render target texture will be used as a render target.
        Data.Material.CommandList.ResourceBarrierTransition(Data.MSAARenderTargetTexture, ResourceStates.Present, ResourceStates.RenderTarget);

        Data.Material.CommandList.BeginEvent("Frame");

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

            Data.Material.CommandList.BeginRenderPass(renderPassDesc, depthStencil);
        }
        else
        {
            // Clear the render target view and depth stencil view with the set color.
            Data.Material.CommandList.ClearRenderTargetView(Data.MSAARenderTargetView.GetCPUDescriptorHandleForHeapStart(), clearColor);
            Data.Material.CommandList.ClearDepthStencilView(Data.DepthStencilView.GetCPUDescriptorHandleForHeapStart(), ClearFlags.Depth | ClearFlags.Stencil, 1.0f, 0);

            // Set the render target and depth stencil view for the device context.
            Data.Material.CommandList.OMSetRenderTargets(Data.MSAARenderTargetView.GetCPUDescriptorHandleForHeapStart(), Data.DepthStencilView.GetCPUDescriptorHandleForHeapStart());
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
    public static bool IsSupported() => D3D12.IsSupported(FeatureLevel.Level_12_0);

    private Result CreateDevice(out Result result)
    {
        // Create a Direct3D 11 device.
        result = D3D12.D3D12CreateDevice(
            null,
            //FeatureLevel.Level_11_0,
            FeatureLevel.Level_12_2,
            out ID3D12Device2 d3d12Device);

        // Check if creating the device was successful.
        if (result.Failure)
            return result;

        // Assign the device to a variable.
        Device = d3d12Device.QueryInterface<ID3D12Device2>();
        Device.Name = "Device";

        return Result.Ok;
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

        try
        {
            bool Debug = true;
            // Create the IDXGIFactory4.
            using IDXGIFactory4 DXGIFactory = DXGI.CreateDXGIFactory2<IDXGIFactory4>(Debug);
            // Creates a swap chain using the swap chain description.
            using IDXGISwapChain1 swapChain1 = forHwnd
                ? DXGIFactory.CreateSwapChainForHwnd(Data.GraphicsQueue, _win32Window.Handle, swapChainDescription)
                : DXGIFactory.CreateSwapChainForComposition(Data.GraphicsQueue, swapChainDescription);

            Data.SwapChain = swapChain1.QueryInterface<IDXGISwapChain3>();
        }
        catch (Exception ex) { throw new Exception(ex.Message); }
    }

    private void GetSwapChainBuffersAndRenderTargetViews()
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

    private void CreateRasterizerState()
    {
        // Create a rasterizer state to fill the triangle using solid fill mode.
        Data.RasterizerState = new()
        {
            FillMode = FillMode.Solid,
            CullMode = CullMode.None,
            AntialiasedLineEnable = true,
            MultisampleEnable = true
        };
    }
}

public sealed partial class Renderer
{
    internal void HandleDeviceLost()
    {
        Result removedReason = Device.DeviceRemovedReason;
        ID3D12DeviceRemovedExtendedData1? dred = Device.QueryInterfaceOrNull<ID3D12DeviceRemovedExtendedData1>();

        if (dred != null)
        {
            if (dred.GetAutoBreadcrumbsOutput1(out DredAutoBreadcrumbsOutput1? dredAutoBreadcrumbsOutput).Success)
            {
                AutoBreadcrumbNode1? currentNode = dredAutoBreadcrumbsOutput.HeadAutoBreadcrumbNode;
                int index = 0;
                while (currentNode != null)
                {
                    string? cmdListName = currentNode.CommandListDebugName;
                    string? cmdQueueName = currentNode.CommandQueueDebugName;
                    int expected = currentNode.BreadcrumbCount;
                    int actual = currentNode.LastBreadcrumbValue.GetValueOrDefault();

                    bool errorOccurred = actual > 0 && actual < expected;

                    if (actual == 0)
                    {
                        // Don't bother logging nodes that don't submit anything
                        currentNode = currentNode.Next;
                        ++index;
                        continue;
                    }

                    currentNode = currentNode.Next;
                    ++index;
                }
            }

            if (dred.GetPageFaultAllocationOutput1(out DredPageFaultOutput1? pageFaultOutput).Success)
            {

            }

            dred.Dispose();
        }
    }
}