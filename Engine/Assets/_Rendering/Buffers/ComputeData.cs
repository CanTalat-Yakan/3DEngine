using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Vortice.Direct3D12;

namespace Engine.Buffers;

public sealed class ComputeData : IDisposable
{
    public ID3D12Resource BufferResource;
    public Texture2D TextureResource;

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    private CommonContext _context;

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
        commandList.ResourceBarrierTransition(BufferResource, ResourceStates.UnorderedAccess, ResourceStates.CopySource);
        commandList.CopyResource(readbackBuffer, BufferResource);
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
        BufferResource?.Dispose();
        BufferResource = null;

        GC.SuppressFinalize(this);
    }
}
