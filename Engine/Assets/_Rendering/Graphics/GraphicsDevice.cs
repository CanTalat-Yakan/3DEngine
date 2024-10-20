﻿using System.Collections.Generic;
using System.Threading;

using Vortice.Direct3D12;
using Vortice.Direct3D12.Debug;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Graphics;

public struct ResourceDelayDestroy(ID3D12Object resource, ulong destroyFrame)
{
    public ID3D12Object Resource = resource;
    public ulong DestroyFrame = destroyFrame;
}

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

    public Queue<ResourceDelayDestroy> DelayDestroy = new();

    public uint ExecuteIndex = 0;
    public ulong ExecuteCount = 3; // Greater equal than 'bufferCount'.

    public static Format SwapChainFormat = Format.R8G8B8A8_UNorm;
    public static Format DepthStencilFormat = Format.D32_Float;
    public List<ID3D12Resource> ScreenResources;
    public ID3D12Resource DepthStencil;

    public uint BufferCount = 3;

    public void Initialize(SizeI size, bool win32Window)
    {
        NativeSize = size;

        CreateDevice();
        CreateGraphicsQueue();
        CreateDescriptorHeaps();
        CreateFence();
        CreateCommandAllocator();
        CreateSwapChain(win32Window);
        CreateDepthStencil();

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
            (uint)Size.Width,
            (uint)Size.Height,
            SwapChain.Description1.Format,
            SwapChain.Description1.Flags).ThrowIfFailed();

        CreateScreenResources();
        CreateDepthStencil();

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

        DepthStencil?.Dispose();
    }
}

public sealed partial class GraphicsDevice : IDisposable
{
    public void Begin() =>
        GetCommandAllocator().Reset();

    public void Present()
    {
        uint syncInterval = (uint)Kernel.Instance.Config.VSync;
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

        uint index = 0;
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
            Width = (uint)Size.Width,
            Height = (uint)Size.Height,
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
        for (uint i = 0; i < BufferCount; i++)
        {
            SwapChain.GetBuffer(i, out ID3D12Resource resource).ThrowIfFailed();

            ScreenResources.Add(resource);
        }
    }

    public void CreateDepthStencil()
    {
        ResourceDescription depthStencilDescription = ResourceDescription.Texture2D(
            DepthStencilFormat,
            (uint)Size.Width,
            (uint)Size.Height,
            arraySize: 1,
            mipLevels: 1);
        depthStencilDescription.Flags |= ResourceFlags.AllowDepthStencil;

        DepthStencil = Device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            depthStencilDescription,
            ResourceStates.DepthWrite,
            new(DepthStencilFormat, 1.0f, 0));
    }
}

public sealed partial class GraphicsDevice : IDisposable
{
    public void CreateRootSignature(RootSignature rootSignature, IList<RootSignatureParameters> types)
    {
        // Static Samplers.
        var samplerDescription = new StaticSamplerDescription[4];

        samplerDescription[0] = new(ShaderVisibility.All, 0, 0)
        {
            AddressU = TextureAddressMode.Mirror,
            AddressV = TextureAddressMode.Mirror,
            AddressW = TextureAddressMode.Mirror,
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

        uint constantBufferViewCount = 0;
        uint shaderResourceViewCount = 0;
        uint unorderedAccessViewCount = 0;

        rootSignature.ConstantBufferView.Clear();
        rootSignature.ShaderResourceView.Clear();
        rootSignature.UnorderedAccessView.Clear();

        for (uint i = 0; i < types.Count; i++)
            switch (types[(int)i])
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

        rootSignature.Resource = Device.CreateRootSignature(rootSignatureDescription);
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
        CommandAllocators[(int)ExecuteIndex];

    public CpuDescriptorHandle GetRenderTargetScreen()
    {
        CpuDescriptorHandle handle = RenderTextureViewHeap.GetTemporaryCPUHandle();
        Device.CreateRenderTargetView(ScreenResources[(int)SwapChain.CurrentBackBufferIndex], null, handle);

        return handle;
    }

    public CpuDescriptorHandle GetDepthStencilScreen()
    {
        CpuDescriptorHandle handle = DepthStencilViewHeap.GetTemporaryCPUHandle();
        Device.CreateDepthStencilView(DepthStencil, null, handle);

        return handle;
    }

    public ID3D12Resource GetScreenResource() =>
        ScreenResources[(int)SwapChain.CurrentBackBufferIndex];
}