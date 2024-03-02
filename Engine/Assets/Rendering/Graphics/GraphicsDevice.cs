using System.Collections.Generic;
using System.Threading;

using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Graphics;

public sealed partial class GraphicsDevice : IDisposable
{
    public SizeI Size => NativeSize.Scale(Kernel.Instance.Config.ResolutionScale);
    public SizeI NativeSize { get; private set; }

    public ID3D12Device2 Device;
    public IDXGIFactory7 Factory;
    public IDXGIAdapter Adapter;

    public IDXGISwapChain3 SwapChain;
    public ID3D12CommandQueue CommandQueue;
    public List<ID3D12CommandAllocator> CommandAllocators;

    public ID3D12Fence Fence;
    public EventWaitHandle WaitHandle;

    public DescriptorHeapX ShaderResourcesHeap = new();
    public DescriptorHeapX DepthStencilViewHeap = new();
    public DescriptorHeapX RenderTextureViewHeap = new();

    public struct ResourceDelayDestroy(ID3D12Object resource, ulong destroyFrame)
    {
        public ID3D12Object Resource = resource;
        public ulong DestroyFrame = destroyFrame;
    }

    public Queue<ResourceDelayDestroy> DelayDestroy = new();

    public int ExecuteIndex = 0;
    public ulong ExecuteCount = 3; // Greater equal than 'bufferCount'.

    public static Format SwapChainFormat = Format.R8G8B8A8_UNorm;
    public static Format DepthStencilFormat = Format.D32_Float;
    public List<ID3D12Resource> ScreenResources;

    public int BufferCount = 3;

    public void Initialize(SizeI size, bool win32Window)
    {
        NativeSize = size;

        CreateDevice();
        CreateGraphicsQueue();
        CreateDescriptorHeaps();
        CreateFence();
        CreateCommandAllocator();
        CreateSwapChain(win32Window);

        ExecuteCount++;
    }

    public void Resize(int newWidth, int newHeight)
    {
        WaitForGPU();

        NativeSize = new(
            Math.Max(1, newWidth),
            Math.Max(1, newHeight));

        DisposeScreenResources();

        SwapChain.ResizeBuffers(
            SwapChain.Description.BufferCount,
            Size.Width,
            Size.Height,
            SwapChain.Description1.Format,
            SwapChain.Description1.Flags).ThrowIfFailed();

        CreateScreenResources();

        Kernel.Instance.Frame();
    }

    public void Dispose()
    {
        WaitForGPU();

        DisposeScreenResources();

        while (DelayDestroy.Count > 0)
            DelayDestroy.Dequeue().Resource?.Dispose();

        foreach (var commandAllocator in CommandAllocators)
            commandAllocator.Dispose();

        Factory?.Dispose();
        CommandQueue?.Dispose();
        ShaderResourcesHeap?.Dispose();
        DepthStencilViewHeap?.Dispose();
        RenderTextureViewHeap?.Dispose();
        SwapChain?.Dispose();
        Fence?.Dispose();
        Device?.Dispose();
        Adapter?.Dispose();
    }

    public void DisposeScreenResources()
    {
        if (ScreenResources is not null)
            foreach (var screenResource in ScreenResources)
                screenResource.Dispose();
    }
}

public sealed partial class GraphicsDevice : IDisposable
{
    public void Begin() =>
        GetCommandAllocator().Reset();

    public void Present()
    {
        int syncInterval = (int)Kernel.Instance.Config.VSync;
        SwapChain.Present(syncInterval, syncInterval == 0 ? PresentFlags.AllowTearing : PresentFlags.None);

        CommandQueue.Signal(Fence, ExecuteCount);

        ExecuteIndex = (ExecuteIndex + 1) % BufferCount;

        if (Fence.CompletedValue < ExecuteCount - (uint)BufferCount + 1)
        {
            Fence.SetEventOnCompletion(ExecuteCount - (uint)BufferCount + 1, WaitHandle);
            WaitHandle.WaitOne();
        }

        DestroyResourceInternal(Fence.CompletedValue);

        ExecuteCount++;
    }

    public void WaitForGPU()
    {
        CommandQueue.Signal(Fence, ExecuteCount);

        Fence.SetEventOnCompletion(ExecuteCount, WaitHandle);
        WaitHandle.WaitOne();

        DestroyResourceInternal(Fence.CompletedValue);

        ExecuteCount++;
    }
}

public sealed partial class GraphicsDevice : IDisposable
{
    private void CreateDevice()
    {
        if (D3D12.D3D12GetDebugInterface<ID3D12Debug>(out var debug).Success)
            debug.EnableDebugLayer();

        DXGI.CreateDXGIFactory1(out Factory).ThrowIfFailed();

        int index = 0;
        while (true)
        {
            var result = Factory.EnumAdapterByGpuPreference(index, GpuPreference.HighPerformance, out Adapter);
            if (result.Success)
                break;

            index++;
        }

        D3D12.D3D12CreateDevice(Adapter, out Device).ThrowIfFailed();
        Device.Name = "Device";
    }

    private void CreateGraphicsQueue()
    {
        CommandQueue = Device.CreateCommandQueue(CommandListType.Direct);
        CommandQueue.Name = "Command Queue";
    }

    private void CreateDescriptorHeaps()
    {
        const int CONSTANTBUFFERVIEW_VIEWSHADERRESOURCEVIEW_UNORDEREDACCESSVIEW_DESCRIPTORCOUNT = 65536;
        DescriptorHeapDescription descriptorHeapDescription = new()
        {
            DescriptorCount = CONSTANTBUFFERVIEW_VIEWSHADERRESOURCEVIEW_UNORDEREDACCESSVIEW_DESCRIPTORCOUNT,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            Flags = DescriptorHeapFlags.ShaderVisible,
            NodeMask = 0,
        };
        ShaderResourcesHeap.Initialize(this, descriptorHeapDescription);

        descriptorHeapDescription = new()
        {
            DescriptorCount = 64,
            Type = DescriptorHeapType.DepthStencilView,
            Flags = DescriptorHeapFlags.None,
        };
        DepthStencilViewHeap.Initialize(this, descriptorHeapDescription);

        descriptorHeapDescription = new()
        {
            DescriptorCount = 64,
            Type = DescriptorHeapType.RenderTargetView,
            Flags = DescriptorHeapFlags.None,
        };
        RenderTextureViewHeap.Initialize(this, descriptorHeapDescription);
    }

    private void CreateFence()
    {
        WaitHandle = new EventWaitHandle(false, EventResetMode.AutoReset);

        Device.CreateFence(ExecuteCount, FenceFlags.None, out Fence).ThrowIfFailed();
        Fence.Name = "Fence";
    }

    private void CreateCommandAllocator()
    {
        CommandAllocators = new();
        for (int i = 0; i < BufferCount; i++)
        {
            Device.CreateCommandAllocator(CommandListType.Direct, out ID3D12CommandAllocator commandAllocator).ThrowIfFailed();
            commandAllocator.Name = "CommandAllocator " + i;

            CommandAllocators.Add(commandAllocator);
        }
    }

    private void CreateSwapChain(bool forHwnd)
    {
        SwapChainDescription1 swapChainDescription = new()
        {
            Width = Size.Width,
            Height = Size.Height,
            Format = SwapChainFormat,
            Stereo = false,
            SampleDescription = SampleDescription.Default,
            BufferUsage = Usage.RenderTargetOutput,
            BufferCount = BufferCount,
            SwapEffect = SwapEffect.FlipDiscard,
            Flags = SwapChainFlags.AllowTearing,
            Scaling = Scaling.Stretch,
            AlphaMode = AlphaMode.Ignore,
        };
        using IDXGISwapChain1 swapChain = forHwnd
            ? Factory.CreateSwapChainForHwnd(CommandQueue, AppWindow.Win32Window.Handle, swapChainDescription)
            : Factory.CreateSwapChainForComposition(CommandQueue, swapChainDescription);

        SwapChain = swapChain.QueryInterface<IDXGISwapChain3>();

        CreateScreenResources();
    }

    private void CreateScreenResources()
    {
        ScreenResources = new();
        for (int i = 0; i < BufferCount; i++)
        {
            SwapChain.GetBuffer(i, out ID3D12Resource resource).ThrowIfFailed();

            ScreenResources.Add(resource);
        }
    }

    public void CreateDepthStencil()
    {
        Texture2D texture = new()
        {
            Width = Size.Width,
            Height = Size.Height,
            MipLevels = 1,
        };
        Kernel.Instance.Context.RenderTargets["DepthStencil"] = texture;

        GPUUpload upload = new()
        {
            Texture2D = texture,
            IndexFormat = DepthStencilFormat,
            TextureData = null,
        };
        Kernel.Instance.Context.UploadQueue.Enqueue(upload);

        DepthStencilDescription depthStencilDescription = new()
        {
            DepthEnable = true,
            DepthFunc = ComparisonFunction.Less,
            DepthWriteMask = DepthWriteMask.All,
        };

        DepthStencilViewDescription depthStencilViewDescription = new()
        {
            Format = DepthStencilFormat,
            
        };
        
        Device.CreateDepthStencilView(texture.Resource, depthStencilViewDescription, GetDepthStencilScreen());

        //TODO FINISH THE DEPTHSTENCIL CREATION
    }


    //private void CreateDepthStencilView()
    //{
    //    // Set up depth stencil description.
    //    DepthStencilDescription depthStencilDescription = new()
    //    {
    //        DepthEnable = true,
    //        DepthFunc = ComparisonFunction.Less,
    //        DepthWriteMask = DepthWriteMask.All,
    //    };
    //    // Create a depth stencil state from the description.
    //    Data.DepthStencilState = Device.CreateDepthStencilState(depthStencilDescription);

    //    // Create a depth stencil texture description with the specified properties.
    //    Texture2DDescription depthStencilTextureDescription = new()
    //    {
    //        Format = Format.D32_Float, // Set format to D32_Float.
    //        Width = Size.Width,
    //        Height = Size.Height,
    //        ArraySize = 1,
    //        MipLevels = 1,
    //        SampleDescription = new(Config.SupportedSampleCount, Config.QualityLevels),
    //        Usage = ResourceUsage.Default,
    //        BindFlags = BindFlags.DepthStencil,
    //        CPUAccessFlags = CpuAccessFlags.None,
    //        MiscFlags = ResourceOptionFlags.None
    //    };
    //    // Create the depth stencil texture based on the description.
    //    Data.DepthStencilTexture = Device.CreateTexture2D(depthStencilTextureDescription);

    //    // Create a depth stencil view description for the multi sampling.
    //    DepthStencilViewDescription depthStencilViewDescription = new(DepthStencilViewDimension.Texture2DMultisampled, Format.D32_Float);
    //    // Create a depth stencil view for the depth stencil texture.
    //    Data.DepthStencilView = Device.CreateDepthStencilView(Data.DepthStencilTexture, depthStencilViewDescription);
    //}






    //public void CreateDepthStencil(int width, int height)
    //{
    //    // 1. Texture Description
    //    Texture2DDescription depthStencilDesc = new D3D12.Texture2DDescription()
    //    {
    //        Width = width,
    //        Height = height,
    //        MipLevels = 1,
    //        ArraySize = 1,
    //        Format = DXGI.Format.D24_UNorm_S8_UInt, // Common depth-stencil format
    //        SampleDesc = new DXGI.SampleDescription(1, 0),
    //        Usage = D3D12.ResourceUsage.Default,
    //        BindFlags = D3D12.BindFlags.DepthStencil,
    //    };

    //    // 2. Create the Texture
    //    Device.CreateCommittedResource(
    //        new D3D12.HeapProperties(D3D12.HeapType.Default),
    //        D3D12.HeapFlags.None,
    //        depthStencilDesc,
    //        D3D12.ResourceStates.DepthWrite,
    //        new D3D12.ClearValue(DXGI.Format.D24_UNorm_S8_UInt, new D3D12.DepthStencilValue(1.0f, 0)),
    //        out depthStencilTexture
    //    ).ThrowIfFailed();
    //    depthStencilTexture.Name = "DepthStencilTexture"; // Optional: Set a name

    //    // 3. Create Depth-Stencil View
    //    D3D12.DepthStencilViewDescription dsvDesc = new D3D12.DepthStencilViewDescription()
    //    {
    //        Format = DXGI.Format.D24_UNorm_S8_UInt,
    //        ViewDimension = D3D12.DsvDimension.Texture2D
    //    };
    //    Device.CreateDepthStencilView(depthStencilTexture, dsvDesc, DepthStencilViewHeap.GetDescriptorHandle());
    //}

}

public sealed partial class GraphicsDevice : IDisposable
{
    public void CreateRootSignature(RootSignature rootSignature, IList<RootSignatureParameters> types)
    {
        // Static Samplers.
        var samplerDescription = new StaticSamplerDescription[4];

        samplerDescription[0] = new(ShaderVisibility.All, 0, 0)
        {
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            BorderColor = StaticBorderColor.OpaqueBlack,
            ComparisonFunction = ComparisonFunction.Never,
            Filter = Filter.MinMagMipLinear,
            MipLODBias = 0,
            MaxAnisotropy = 0,
            MinLOD = 0,
            MaxLOD = float.MaxValue,
            ShaderVisibility = ShaderVisibility.All,
            RegisterSpace = 0,
            ShaderRegister = 0,
        };
        samplerDescription[1] = samplerDescription[0];
        samplerDescription[2] = samplerDescription[0];
        samplerDescription[3] = samplerDescription[0];

        samplerDescription[1].ShaderRegister = 1;
        samplerDescription[2].ShaderRegister = 2;
        samplerDescription[3].ShaderRegister = 3;

        samplerDescription[1].MaxAnisotropy = 16;
        samplerDescription[1].Filter = Filter.Anisotropic;

        samplerDescription[2].ComparisonFunction = ComparisonFunction.Less;
        samplerDescription[2].Filter = Filter.ComparisonMinMagMipLinear;

        samplerDescription[3].Filter = Filter.MinMagMipPoint;

        var rootParameters = new RootParameter1[types.Count];

        int constantBufferViewCount = 0;
        int shaderResourceViewCount = 0;
        int unorderedAccessViewCount = 0;

        rootSignature.ConstantBufferView.Clear();
        rootSignature.ShaderResourceView.Clear();
        rootSignature.UnorderedAccessView.Clear();

        for (int i = 0; i < types.Count; i++)
            switch (types[i])
            {
                case RootSignatureParameters.ConstantBufferView:
                    rootParameters[i] = new RootParameter1(RootParameterType.ConstantBufferView, new RootDescriptor1(constantBufferViewCount, 0), ShaderVisibility.All);
                    rootSignature.ConstantBufferView[constantBufferViewCount] = i;
                    constantBufferViewCount++;
                    break;
                case RootSignatureParameters.ConstantBufferViewTable:
                    rootParameters[i] = new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ConstantBufferView, 1, constantBufferViewCount)), ShaderVisibility.All);
                    rootSignature.ConstantBufferView[constantBufferViewCount] = i;
                    constantBufferViewCount++;
                    break;
                case RootSignatureParameters.ShaderResourceView:
                    rootParameters[i] = new RootParameter1(RootParameterType.ShaderResourceView, new RootDescriptor1(shaderResourceViewCount, 0), ShaderVisibility.All);
                    rootSignature.ShaderResourceView[shaderResourceViewCount] = i;
                    shaderResourceViewCount++;
                    break;
                case RootSignatureParameters.ShaderResourceViewTable:
                    rootParameters[i] = new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.ShaderResourceView, 1, shaderResourceViewCount)), ShaderVisibility.All);
                    rootSignature.ShaderResourceView[shaderResourceViewCount] = i;
                    shaderResourceViewCount++;
                    break;
                case RootSignatureParameters.UnorderedAccessView:
                    rootParameters[i] = new RootParameter1(RootParameterType.UnorderedAccessView, new RootDescriptor1(unorderedAccessViewCount, 0), ShaderVisibility.All);
                    rootSignature.UnorderedAccessView[unorderedAccessViewCount] = i;
                    unorderedAccessViewCount++;
                    break;
                case RootSignatureParameters.UnorderedAccessViewTable:
                    rootParameters[i] = new RootParameter1(new RootDescriptorTable1(new DescriptorRange1(DescriptorRangeType.UnorderedAccessView, 1, unorderedAccessViewCount)), ShaderVisibility.All);
                    rootSignature.UnorderedAccessView[unorderedAccessViewCount] = i;
                    unorderedAccessViewCount++;
                    break;
            }

        RootSignatureDescription1 rootSignatureDescription = new()
        {
            StaticSamplers = samplerDescription,
            Flags = RootSignatureFlags.AllowInputAssemblerInputLayout,
            Parameters = rootParameters,
        };

        rootSignature.Resource = Device.CreateRootSignature<ID3D12RootSignature>(0, rootSignatureDescription);
    }

    public void CreateRenderTexture(Texture2D texture)
    {
        ResourceDescription resourceDescription = new()
        {
            Width = (ulong)texture.Width,
            Height = texture.Height,
            MipLevels = 1,
            SampleDescription = new SampleDescription(1, 0),
            Dimension = ResourceDimension.Texture2D,
            DepthOrArraySize = 1,
            Format = texture.Format,
        };

        if (texture.DepthStencilViewFormat != 0)
        {
            DestroyResource(texture.Resource);
            resourceDescription.Flags = ResourceFlags.AllowDepthStencil;

            Device.CreateCommittedResource<ID3D12Resource>(HeapProperties.DefaultHeapProperties,
                 HeapFlags.None,
                 resourceDescription,
                 ResourceStates.GenericRead,
                 new ClearValue(texture.DepthStencilViewFormat, new DepthStencilValue(1.0f, 0)),
                 out texture.Resource).ThrowIfFailed();

            if (texture.DepthStencilView is null)
            {
                DescriptorHeapDescription descriptorHeapDescription = new()
                {
                    DescriptorCount = 1,
                    Type = DescriptorHeapType.DepthStencilView,
                    Flags = DescriptorHeapFlags.None,
                    NodeMask = 0,
                };
                Device.CreateDescriptorHeap(descriptorHeapDescription, out texture.DepthStencilView).ThrowIfFailed();
            }

            Device.CreateDepthStencilView(texture.Resource, null, texture.DepthStencilView.GetCPUDescriptorHandleForHeapStart());
        }
        else if (texture.RenderTextureViewFormat != 0)
        {
            DestroyResource(texture.Resource);
            resourceDescription.Flags = ResourceFlags.AllowRenderTarget | ResourceFlags.AllowUnorderedAccess;

            Device.CreateCommittedResource<ID3D12Resource>(HeapProperties.DefaultHeapProperties,
                 HeapFlags.None,
                 resourceDescription,
                 ResourceStates.GenericRead,
                 new ClearValue(texture.DepthStencilViewFormat, new Color4(0, 0, 0, 0)),
                 out texture.Resource).ThrowIfFailed();

            if (texture.RenderTargetView is null)
            {
                DescriptorHeapDescription descriptorHeapDescription = new()
                {
                    DescriptorCount = 1,
                    Type = DescriptorHeapType.RenderTargetView,
                    Flags = DescriptorHeapFlags.None,
                    NodeMask = 0,
                };
                Device.CreateDescriptorHeap(descriptorHeapDescription, out texture.RenderTargetView).ThrowIfFailed();
            }

            Device.CreateRenderTargetView(texture.Resource, null, texture.RenderTargetView.GetCPUDescriptorHandleForHeapStart());
        }
        else
            throw new NotImplementedException();

        texture.ResourceStates = ResourceStates.GenericRead;
    }

    public void CreateUploadBuffer(UploadBuffer uploadBuffer, int size)
    {
        DestroyResource(uploadBuffer.Resource);

        uploadBuffer.Resource = Device.CreateCommittedResource<ID3D12Resource>(
            HeapProperties.UploadHeapProperties,
            HeapFlags.None,
            ResourceDescription.Buffer(new ResourceAllocationInfo((ulong)size, 0)),
            ResourceStates.GenericRead);
        uploadBuffer.Size = size;
    }

    public void DestroyResource(ID3D12Object resource)
    {
        if (resource is not null)
            DelayDestroy.Enqueue(new ResourceDelayDestroy(resource, ExecuteCount));
    }

    private void DestroyResourceInternal(ulong completedFrame)
    {
        while (DelayDestroy.Count > 0)
            if (DelayDestroy.Peek().DestroyFrame <= completedFrame)
            {
                var p = DelayDestroy.Dequeue();
                p.Resource?.Dispose();
            }
            else
                break;
    }
}

public sealed partial class GraphicsDevice : IDisposable
{
    public static int GetMegabytesInByte(int megabytes) =>
        megabytes * 1024 * 1024;

    public static int GetSizeInByte(Format format) =>
        format switch
        {
            Format.R16_UInt => 2,

            Format.R32_UInt or
            Format.R32_Float or
            Format.R8G8B8A8_UNorm => 4,

            Format.R32G32_Float => 8,

            Format.R32G32B32_Float => 12,

            _ => 0,
        };

    public static uint GetBitsPerPixel(Format format) =>
        format switch
        {
            Format.R32G32B32A32_Typeless or
            Format.R32G32B32A32_Float or
            Format.R32G32B32A32_UInt or
            Format.R32G32B32A32_SInt => 128,

            Format.R32G32B32_Typeless or
            Format.R32G32B32_Float or
            Format.R32G32B32_UInt or
            Format.R32G32B32_SInt => 96,

            Format.R16G16B16A16_Typeless or
            Format.R16G16B16A16_Float or
            Format.R16G16B16A16_UNorm or
            Format.R16G16B16A16_UInt or
            Format.R16G16B16A16_SNorm or
            Format.R16G16B16A16_SInt or
            Format.R32G32_Typeless or
            Format.R32G32_Float or
            Format.R32G32_UInt or
            Format.R32G32_SInt or
            Format.R32G8X24_Typeless or
            Format.D32_Float_S8X24_UInt or
            Format.R32_Float_X8X24_Typeless or
            Format.X32_Typeless_G8X24_UInt or
            Format.Y416 or
            Format.Y210 or
            Format.Y216 => 64,

            Format.R10G10B10A2_Typeless or
            Format.R10G10B10A2_UNorm or
            Format.R10G10B10A2_UInt or
            Format.R11G11B10_Float or
            Format.R8G8B8A8_Typeless or
            Format.R8G8B8A8_UNorm or
            Format.R8G8B8A8_UNorm_SRgb or
            Format.R8G8B8A8_UInt or
            Format.R8G8B8A8_SNorm or
            Format.R8G8B8A8_SInt or
            Format.R16G16_Typeless or
            Format.R16G16_Float or
            Format.R16G16_UNorm or
            Format.R16G16_UInt or
            Format.R16G16_SNorm or
            Format.R16G16_SInt or
            Format.R32_Typeless or
            Format.D32_Float or
            Format.R32_Float or
            Format.R32_UInt or
            Format.R32_SInt or
            Format.R24G8_Typeless or
            Format.D24_UNorm_S8_UInt or
            Format.R24_UNorm_X8_Typeless or
            Format.X24_Typeless_G8_UInt or
            Format.R9G9B9E5_SharedExp or
            Format.R8G8_B8G8_UNorm or
            Format.G8R8_G8B8_UNorm or
            Format.B8G8R8A8_UNorm or
            Format.B8G8R8X8_UNorm or
            Format.R10G10B10_Xr_Bias_A2_UNorm or
            Format.B8G8R8A8_Typeless or
            Format.B8G8R8A8_UNorm_SRgb or
            Format.B8G8R8X8_Typeless or
            Format.B8G8R8X8_UNorm_SRgb or
            Format.AYUV or
            Format.Y410 or
            Format.YUY2 => 32,

            Format.P010 or
            Format.P016 => 24,

            Format.R8G8_Typeless or
            Format.R8G8_UNorm or
            Format.R8G8_UInt or
            Format.R8G8_SNorm or
            Format.R8G8_SInt or
            Format.R16_Typeless or
            Format.R16_Float or
            Format.D16_UNorm or
            Format.R16_UNorm or
            Format.R16_UInt or
            Format.R16_SNorm or
            Format.R16_SInt or
            Format.B5G6R5_UNorm or
            Format.B5G5R5A1_UNorm or
            Format.A8P8 or
            Format.B4G4R4A4_UNorm => 16,

            Format.NV12 or
            Format.Opaque420 or
            Format.NV11 => 12,

            Format.R8_Typeless or
            Format.R8_UNorm or
            Format.R8_UInt or
            Format.R8_SNorm or
            Format.R8_SInt or
            Format.A8_UNorm or
            Format.AI44 or
            Format.IA44 or
            Format.P8 => 8,

            Format.R1_UNorm => 1,

            Format.BC1_Typeless or
            Format.BC1_UNorm or
            Format.BC1_UNorm_SRgb or
            Format.BC4_Typeless or
            Format.BC4_UNorm or
            Format.BC4_SNorm => 4,

            Format.BC2_Typeless or
            Format.BC2_UNorm or
            Format.BC2_UNorm_SRgb or
            Format.BC3_Typeless or
            Format.BC3_UNorm or
            Format.BC3_UNorm_SRgb or
            Format.BC5_Typeless or
            Format.BC5_UNorm or
            Format.BC5_SNorm or
            Format.BC6H_Typeless or
            Format.BC6H_Uf16 or
            Format.BC6H_Sf16 or
            Format.BC7_Typeless or
            Format.BC7_UNorm or
            Format.BC7_UNorm_SRgb => 8,

            _ => 0,
        };

    public ID3D12CommandAllocator GetCommandAllocator() =>
        CommandAllocators[ExecuteIndex];

    public CpuDescriptorHandle GetRenderTargetScreen()
    {
        CpuDescriptorHandle handle = RenderTextureViewHeap.GetTemporaryCPUHandle();
        var resource = ScreenResources[SwapChain.CurrentBackBufferIndex];

        Device.CreateRenderTargetView(resource, null, handle);

        return handle;
    }
    public CpuDescriptorHandle GetDepthStencilScreen()
    {
        CpuDescriptorHandle handle = RenderTextureViewHeap.GetTemporaryCPUHandle();
        var resource = Kernel.Instance.Context.RenderTargets["DepthStencil"].Resource;

        Device.CreateRenderTargetView(resource, null, handle);

        return handle;
    }

    public ID3D12Resource GetScreenResource() =>
        ScreenResources[SwapChain.CurrentBackBufferIndex];
}