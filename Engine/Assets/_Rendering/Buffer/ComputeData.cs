﻿using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Direct3D12;

namespace Engine.Buffer;

public sealed class ComputeData : IDisposable
{
    public UploadBuffer UploadBuffer = new();
    public ID3D12Resource UavBufferResource;
    public Texture2D TextureResource;

    private CommonContext _context;
    public CommonContext Context => _context ??= Kernel.Instance.Context;

    // Method to set the data buffer and upload using UploadBuffer
    public void SetData<T>(T[] data) where T : struct
    {
        // Step 1: Create UAV buffer for compute operations
        int bufferSize = Unsafe.SizeOf<T>() * data.Length;

        UavBufferResource = Context.GraphicsDevice.Device.CreateCommittedResource(
            new HeapProperties(HeapType.Default),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)bufferSize, ResourceFlags.AllowUnorderedAccess),
            ResourceStates.CopyDest
        );

        // Step 2: Upload data using UploadBuffer
        UploadBuffer.Initialize(Context.GraphicsDevice, bufferSize);
        UploadBuffer.Upload(data, out uint offset);

        // Step 3: Copy data from UploadBuffer to the UAV buffer
        var commandList = Context.GraphicsDevice.Device.CreateCommandList<ID3D12GraphicsCommandList5>(0, CommandListType.Compute, Context.GraphicsDevice.GetComputeCommandAllocator(), null);
        commandList.CopyBufferRegion(UavBufferResource, 0, UploadBuffer.Resource, offset, (ulong)bufferSize);
        commandList.ResourceBarrierTransition(UavBufferResource, ResourceStates.CopyDest, ResourceStates.UnorderedAccess);
        commandList.Close();
        Context.GraphicsDevice.CommandQueue.ExecuteCommandList(commandList);
        commandList.Dispose();
    }

    // Method to set Texture2D for compute shader usage
    public void SetTexture(Texture2D texture2D)
    {
        if (texture2D == null || texture2D.Resource == null)
        {
            throw new ArgumentNullException(nameof(texture2D), "The provided Texture2D must be initialized and have a valid resource.");
        }

        TextureResource = texture2D;
        TextureResource.StateChange(Context.ComputeContext.CommandList, ResourceStates.UnorderedAccess);
    }

    // Method to read data back from the UAV buffer
    public T[] ReadData<T>(int elementCount) where T : struct
    {
        int bufferSize = Unsafe.SizeOf<T>() * elementCount;

        // Step 1: Create a readback buffer
        var readbackBuffer = Context.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
            new HeapProperties(HeapType.Readback),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)bufferSize),
            ResourceStates.CopyDest
        );

        // Step 2: Copy from UAV buffer to readback buffer
        var commandList = Context.GraphicsDevice.Device.CreateCommandList<ID3D12GraphicsCommandList5>(0, CommandListType.Compute, Context.GraphicsDevice.GetComputeCommandAllocator(), null);
        commandList.ResourceBarrierTransition(UavBufferResource, ResourceStates.UnorderedAccess, ResourceStates.CopySource);
        commandList.CopyResource(readbackBuffer, UavBufferResource);
        commandList.Close();
        Context.GraphicsDevice.CommandQueue.ExecuteCommandList(commandList);
        commandList.Dispose();

        // Step 3: Read back the data from the readback buffer
        T[] readbackData = new T[elementCount];
        IntPtr mappedData = new();
        unsafe
        {
            readbackBuffer.Map(0, null, (void*)mappedData);
            for (int i = 0; i < elementCount; i++)
                readbackData[i] = Marshal.PtrToStructure<T>(mappedData + i * Marshal.SizeOf<T>());
            readbackBuffer.Unmap(0);
        }

        return readbackData;
    }

    // Dispose resources to prevent memory leaks
    public void Dispose()
    {
        UploadBuffer?.Dispose();
        UploadBuffer = null;

        UavBufferResource?.Dispose();
        UavBufferResource = null;

        TextureResource?.Dispose();
        TextureResource = null;

        GC.SuppressFinalize(this);
    }
}