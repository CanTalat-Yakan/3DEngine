using System.Runtime.CompilerServices;
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

public unsafe sealed partial class RingUploadBuffer : UploadBuffer
{
    public IntPtr CPUResourcePointer;
    public ulong GPUResourcePointer;

    public int AllocateIndex = 0;

    public GraphicsContext GraphicsContext => _graphicsContext ??= Kernel.Instance.Context.GraphicsContext;
    public GraphicsContext _graphicsContext;

    public void Initialize(GraphicsDevice device, int size)
    {
        Size = size;
        device.CreateUploadBuffer(this, size);

        void* pointer = null;
        Resource.Map(0, &pointer);

        CPUResourcePointer = new IntPtr(pointer);
        GPUResourcePointer = Resource.GPUVirtualAddress;
    }

    public void Upload<T>(Span<T> data, out int offset) where T : struct
    {
        int size = data.Length * Marshal.SizeOf(typeof(T));
        int afterAllocateIndex = AllocateIndex + ((size + 255) & ~255);
        if (afterAllocateIndex > Size)
        {
            AllocateIndex = 0;
            afterAllocateIndex = AllocateIndex + ((size + 255) & ~255);
        }

        data.CopyTo(new Span<T>((CPUResourcePointer + AllocateIndex).ToPointer(), data.Length));

        offset = AllocateIndex;
        AllocateIndex = afterAllocateIndex % Size;
    }

    public void Upload<T>(T data, out int offset)
    {
        int size = Marshal.SizeOf(typeof(T));
        int afterAllocateIndex = AllocateIndex + ((size + 255) & ~255);
        if (afterAllocateIndex > Size)
        {
            AllocateIndex = 0;
            afterAllocateIndex = AllocateIndex + ((size + 255) & ~255);
        }

        Unsafe.Copy((void*)(CPUResourcePointer + AllocateIndex), ref data);

        offset = AllocateIndex;
        AllocateIndex = afterAllocateIndex % Size;
    }
}

public unsafe sealed partial class RingUploadBuffer : UploadBuffer
{
    public void SetConstantBufferView(int offset, int slot) =>
        GraphicsContext.SetConstantBufferView(this, offset, slot);

    public void UploadIndexBuffer(MeshInfo mesh, Span<byte> index, Format indexFormat, int? overrideSizeInByte = null)
    {
        int indexSizeInByte = overrideSizeInByte is not null ? overrideSizeInByte.Value : index.Length;
        int indexCount = indexSizeInByte / GraphicsDevice.GetSizeInByte(indexFormat);

        if (mesh.IndexBufferResource is null
         || mesh.IndexFormat != indexFormat
         || mesh.IndexCount != indexCount
         || mesh.IndexSizeInByte != indexSizeInByte)
        {
            mesh.IndexFormat = indexFormat;
            mesh.IndexCount = indexCount;
            mesh.IndexSizeInByte = indexSizeInByte;

            GraphicsContext.GraphicsDevice.DestroyResource(mesh.IndexBufferResource);

            mesh.IndexBufferResource = GraphicsContext.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer((ulong)indexSizeInByte),
                ResourceStates.CopyDest);
        }

        Upload(index, out var offset);

        GraphicsContext.CommandList.CopyBufferRegion(mesh.IndexBufferResource, 0, Resource, (ulong)offset, (ulong)indexSizeInByte);
        GraphicsContext.CommandList.ResourceBarrierTransition(mesh.IndexBufferResource, ResourceStates.CopyDest, ResourceStates.GenericRead);
    }

    public void UploadVertexBuffer(MeshInfo mesh, Span<byte> vertex, int? overrideSizeInByte = null)
    {
        var vertexSizeInByte = overrideSizeInByte is not null ? overrideSizeInByte.Value : vertex.Length;

        if (mesh.VertexBufferResource is null
         || mesh.VertexSizeInByte != vertexSizeInByte)
        {
            mesh.VertexSizeInByte = vertexSizeInByte;

            GraphicsContext.GraphicsDevice.DestroyResource(mesh.VertexBufferResource);

            mesh.VertexBufferResource = GraphicsContext.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer((ulong)vertexSizeInByte),
                ResourceStates.CopyDest);
        }

        Upload(vertex, out var offset);

        GraphicsContext.CommandList.CopyBufferRegion(mesh.VertexBufferResource, 0, Resource, (ulong)offset, (ulong)vertexSizeInByte);
        GraphicsContext.CommandList.ResourceBarrierTransition(mesh.VertexBufferResource, ResourceStates.CopyDest, ResourceStates.GenericRead);
    }
}