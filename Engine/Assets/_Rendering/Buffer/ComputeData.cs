using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Vortice.Direct3D12;

namespace Engine.Buffer;

public sealed class ComputeData : IDisposable
{
    public UploadBuffer UploadBuffer = new();

    public ID3D12Resource BufferResource;
    public Texture2D TextureResource;

    private CommonContext _context;
    public CommonContext Context => _context ??= Kernel.Instance.Context;

    // Method to set the data buffer and upload using UploadBuffer
    public void SetData<T>(T[] data, uint slot = 0) where T : struct
    {
        // Step 1: Create UAV buffer for compute operations
        int bufferSize = Unsafe.SizeOf<T>() * data.Length;

        BufferResource = Context.GraphicsDevice.Device.CreateCommittedResource(
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
        commandList.CopyBufferRegion(BufferResource, 0, UploadBuffer.Resource, offset, (ulong)bufferSize);
        commandList.ResourceBarrierTransition(BufferResource, ResourceStates.CopyDest, ResourceStates.UnorderedAccess);
        commandList.Close();
        Context.GraphicsDevice.CommandQueue.ExecuteCommandList(commandList);
        Context.ComputeContext.SetUnorderedAccessView(BufferResource, offset, slot);
        commandList.Dispose();
    }

    // Method to set Texture2D for compute shader usage
    public void SetTexture(Texture2D texture2D, uint slot)
    {
        if (texture2D is null || texture2D.Resource is null)
            throw new ArgumentNullException(nameof(texture2D), "The provided Texture2D must be initialized and have a valid resource.");

        TextureResource = texture2D;
        TextureResource.StateChange(Context.ComputeContext.CommandList, ResourceStates.UnorderedAccess);
        Context.ComputeContext.SetShaderResourceView(texture2D, slot);
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
        UploadBuffer?.Dispose();
        UploadBuffer = null;

        BufferResource?.Dispose();
        BufferResource = null;

        TextureResource?.Dispose();
        TextureResource = null;

        GC.SuppressFinalize(this);
    }
}
