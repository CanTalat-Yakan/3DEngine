﻿using System.IO;
using System.Runtime.CompilerServices;
using System.Threading;
using Vortice.Direct3D12;

namespace Engine.Graphics;

public sealed partial class ComputeContext : IDisposable
{
    public GraphicsDevice GraphicsDevice;

    public ID3D12GraphicsCommandList5 CommandList;
    public ID3D12CommandAllocator ComputeCommandAllocator;

    public RootSignature CurrentRootSignature;

    public ComputePipelineStateObject ComputePipelineStateObject;
    public ComputePipelineStateObjectDescription ComputePipelineStateObjectDescription;

    private ID3D12Fence ComputeFence;
    private EventWaitHandle WaitHandle = new(false, EventResetMode.AutoReset);

    private ulong computeFenceValue = 1;

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;

        GraphicsDevice.Device.CreateCommandAllocator(CommandListType.Compute, out ComputeCommandAllocator).ThrowIfFailed();
        ComputeCommandAllocator.Name = "ComputeCommandAllocator";

        GraphicsDevice.Device.CreateCommandList(0, CommandListType.Compute, ComputeCommandAllocator, null, out CommandList).ThrowIfFailed();
        CommandList.Close();

        // Create the fence for synchronization
        GraphicsDevice.Device.CreateFence(0, FenceFlags.None, out ComputeFence).ThrowIfFailed();
    }

    public void Dispose()
    {
        CommandList?.Dispose();
        CommandList = null;

        GC.SuppressFinalize(this);
    }
}

public sealed partial class ComputeContext : IDisposable
{
    public void SetData<T>(out ComputeData computeData, T[] data, uint slot = 0) where T : struct
    {
        // Step 1: Create UAV buffer for compute operations
        int bufferSize = Unsafe.SizeOf<T>() * data.Length;

        computeData = new();
        computeData.BufferResource = GraphicsDevice.Device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)bufferSize, ResourceFlags.AllowUnorderedAccess),
            ResourceStates.CopyDest
        );

        // Step 2: Upload data using UploadBuffer
        var uploadBuffer = Kernel.Instance.Context.UploadBuffer;
        uploadBuffer.Upload(data, out uint offset);

        // Step 3: Copy data from UploadBuffer to the UAV buffer
        var commandList = GraphicsDevice.Device.CreateCommandList<ID3D12GraphicsCommandList5>(0, CommandListType.Compute, ComputeCommandAllocator, null);
        commandList.CopyBufferRegion(computeData.BufferResource, 0, uploadBuffer.Resource, offset, (ulong)bufferSize);
        commandList.ResourceBarrierTransition(computeData.BufferResource, ResourceStates.CopyDest, ResourceStates.UnorderedAccess);
        commandList.Close();

        GraphicsDevice.CommandQueue.ExecuteCommandList(commandList);
        SetUnorderedAccessView(computeData.BufferResource, offset, slot);

        commandList.Dispose();
    }

    public void SetTexture(out ComputeData computeData, uint width, uint height, Vortice.DXGI.Format format, string name, uint slot)
    {
        uint mipLevels = 1;
        Texture2D texture2D = new()
        {
            Width = width,
            Height = height,
            MipLevels = mipLevels,
            Format = format,
            Name = name,
        };

        Assets.RenderTextures[name] = texture2D;

        // Calculate the total size and get copyable footprints
        var resourceDescription = ResourceDescription.Texture2D(texture2D.Format, texture2D.Width, texture2D.Height, arraySize: 1, mipLevels: 0);

        ulong totalSize = 0;
        ulong[] rowSizesInBytes = new ulong[mipLevels];
        uint[] numRows = new uint[mipLevels];
        var layouts = new PlacedSubresourceFootPrint[mipLevels];

        GraphicsDevice.Device.GetCopyableFootprints(resourceDescription, 0, mipLevels, 0, layouts, numRows, rowSizesInBytes, out totalSize);

        Kernel.Instance.Context.UploadBuffer.UploadTexture(CommandList, texture2D, [new byte[totalSize]], layouts, numRows, rowSizesInBytes);

        computeData = new();
        computeData.TextureResource = texture2D;
        computeData.TextureResource.StateChange(CommandList, ResourceStates.UnorderedAccess);

        SetShaderResourceView(texture2D, slot);
    }

    public ReadOnlyMemory<byte> LoadComputeShader(DxcShaderStage shaderStage, string localPath, string entryPoint, bool fromResources = false)
    {
        localPath = fromResources ? AssetPaths.RESOURCECOMPUTESHADER + localPath : AssetPaths.ASSETS + localPath;

        string directory = Path.GetDirectoryName(localPath);
        string shaderSource = File.ReadAllText(localPath);

        using (ShaderIncludeHandler includeHandler = new(directory))
        {
            using IDxcResult results = DxcCompiler.Compile(shaderStage, shaderSource, entryPoint, includeHandler: includeHandler);
            if (results.GetStatus().Failure)
                throw new Exception(results.GetErrors());

            return results.GetObjectBytecodeMemory();
        }
    }
}

public sealed partial class ComputeContext : IDisposable
{
    public void BeginCommand()
    {
        // Wait for previous compute commands to finish
        WaitForCompute();

        ComputeCommandAllocator.Reset();
        CommandList.Reset(ComputeCommandAllocator);
    }

    public void EndCommand() =>
        CommandList.Close();

    public void WaitForCompute()
    {
        // Check if GPU has completed the compute work
        if (ComputeFence.CompletedValue < computeFenceValue)
        {
            // Wait until the GPU completes work
            ComputeFence.SetEventOnCompletion(computeFenceValue, WaitHandle.SafeWaitHandle.DangerousGetHandle());
            WaitHandle.WaitOne();
        }

        // Increment fence value for the next set of compute commands
        computeFenceValue++;
    }

    public void Execute()
    {
        GraphicsDevice.CommandQueue.ExecuteCommandList(CommandList);

        // Signal the fence for synchronization
        GraphicsDevice.CommandQueue.Signal(ComputeFence, computeFenceValue);
    }

    public void SetPipelineState(ComputePipelineStateObject computePipelineStateObject, ComputePipelineStateObjectDescription computePipelineStateObjectDescription)
    {
        ComputePipelineStateObject = computePipelineStateObject;
        ComputePipelineStateObjectDescription = computePipelineStateObjectDescription;
    }

    public void SetRootSignature(RootSignature rootSignature)
    {
        CurrentRootSignature = rootSignature;
        CommandList.SetComputeRootSignature(rootSignature.Resource);
    }

    public void SetConstantBufferView(ID3D12Resource resource, uint offset, uint slot) =>
        CommandList.SetGraphicsRootConstantBufferView(CurrentRootSignature.ConstantBufferView[slot], resource.GPUVirtualAddress + offset);

    public void SetUnorderedAccessView(ID3D12Resource resource, uint offset, uint slot) =>
        CommandList.SetGraphicsRootUnorderedAccessView(CurrentRootSignature.UnorderedAccessView[slot], resource.GPUVirtualAddress + offset);

    public void SetShaderResourceView(Texture2D texture2D, uint slot)
    {
        texture2D.StateChange(CommandList, ResourceStates.UnorderedAccess);

        CommandList.SetGraphicsRootDescriptorTable(CurrentRootSignature.ShaderResourceView[slot], GraphicsDevice.GetShaderResourceHandleGPU(texture2D));
    }
}