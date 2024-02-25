using System.Runtime.InteropServices;

using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.Buffer;

public class UploadBuffer : IDisposable
{
    public ID3D12Resource Resource;
    public int Size;

    public void Dispose() =>
        Resource?.Dispose();
}

public unsafe sealed class RingUploadBuffer : UploadBuffer
{
    public IntPtr CPUResourcePointer;
    public ulong GPUResourcePointer;

    public int AllocateIndex = 0;

    public void Initialize(GraphicsDevice device, int size)
    {
        Size = size;
        device.CreateUploadBuffer(this, size);

        void* pointer = null;
        Resource.Map(0, &pointer);

        CPUResourcePointer = new IntPtr(pointer);
        GPUResourcePointer = Resource.GPUVirtualAddress;
    }

    public unsafe int Upload<T>(Span<T> data) where T : struct
    {
        int size = data.Length * Marshal.SizeOf(typeof(T));
        int afterAllocateIndex = AllocateIndex + ((size + 255) & ~255);
        if (afterAllocateIndex > Size)
        {
            AllocateIndex = 0;
            afterAllocateIndex = AllocateIndex + ((size + 255) & ~255);
        }

        data.CopyTo(new Span<T>((CPUResourcePointer + AllocateIndex).ToPointer(), data.Length));

        int offset = AllocateIndex;
        AllocateIndex = afterAllocateIndex % Size;

        return offset;
    }

    public void SetConstantBufferView(GraphicsContext graphicsContext, int offset, int slot) =>
        graphicsContext.SetConstantBufferView(this, offset, slot);

    public void UploadMeshIndex(GraphicsContext context, MeshInfo mesh, Span<byte> index, Format indexFormat)
    {
        var commandList = context.CommandList;

        int uploadOffset = Upload(index);
        if (mesh.IndexFormat != indexFormat
         || mesh.IndexCount != index.Length / (indexFormat == Format.R32_UInt ? 4 : 2)
         || mesh.IndexSizeInByte != index.Length)
        {
            mesh.IndexFormat = indexFormat;
            mesh.IndexCount = index.Length / (indexFormat == Format.R32_UInt ? 4 : 2);
            mesh.IndexSizeInByte = index.Length;

            context.GraphicsDevice.DestroyResource(mesh.IndexBufferResource);

            mesh.IndexBufferResource = context.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer((ulong)index.Length),
                ResourceStates.CopyDest);
        }
        else
            commandList.ResourceBarrierTransition(mesh.IndexBufferResource, ResourceStates.GenericRead, ResourceStates.CopyDest);

        commandList.CopyBufferRegion(mesh.IndexBufferResource, 0, Resource, (ulong)uploadOffset, (ulong)index.Length);
        commandList.ResourceBarrierTransition(mesh.IndexBufferResource, ResourceStates.CopyDest, ResourceStates.GenericRead);
    }

    public void UploadVertexBuffer(GraphicsContext context, ref ID3D12Resource resource, Span<byte> vertex)
    {
        var commandList = context.CommandList;

        context.GraphicsDevice.DestroyResource(resource);
        resource = context.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
            HeapProperties.DefaultHeapProperties,
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)vertex.Length),
            ResourceStates.CopyDest);

        int uploadOffset = Upload(vertex);
        commandList.CopyBufferRegion(resource, 0, Resource, (ulong)uploadOffset, (ulong)vertex.Length);
        commandList.ResourceBarrierTransition(resource, ResourceStates.CopyDest, ResourceStates.GenericRead);
    }
}