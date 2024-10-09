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

    public void UploadIndexBuffer(MeshData mesh, Span<byte> index, Format indexFormat, int? overrideSizeInByte = null)
    {
        int indexSizeInByte = overrideSizeInByte ?? index.Length;
        int indexCount = indexSizeInByte / GraphicsDevice.GetSizeInByte(indexFormat);

        bool needRecreateResource = 
            mesh.IndexBufferResource is null 
         || mesh.IndexFormat != indexFormat 
         || indexSizeInByte > mesh.IndexSizeInByte;

        if (needRecreateResource)
        {
            mesh.IndexFormat = indexFormat;
            mesh.IndexSizeInByte = indexSizeInByte;

            GraphicsContext.GraphicsDevice.DestroyResource(mesh.IndexBufferResource);

            mesh.IndexBufferResource = GraphicsContext.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer((ulong)indexSizeInByte),
                ResourceStates.CopyDest);

            mesh.IndexBufferState = ResourceStates.CopyDest;
        }
        else if (mesh.IndexBufferState != ResourceStates.CopyDest)
        {
            // Transition to CopyDest state
            GraphicsContext.CommandList.ResourceBarrierTransition(
                mesh.IndexBufferResource, mesh.IndexBufferState, ResourceStates.CopyDest);
            mesh.IndexBufferState = ResourceStates.CopyDest;
        }

        mesh.IndexCount = indexCount;

        Upload(index, out var offset);

        GraphicsContext.CommandList.CopyBufferRegion(
            mesh.IndexBufferResource, 0, Resource, (ulong)offset, (ulong)indexSizeInByte);

        // Transition to GenericRead state
        GraphicsContext.CommandList.ResourceBarrierTransition(
            mesh.IndexBufferResource, ResourceStates.CopyDest, ResourceStates.GenericRead);
        mesh.IndexBufferState = ResourceStates.GenericRead;
    }

    public void UploadVertexBuffer(MeshData mesh, Span<byte> vertex, int? overrideSizeInByte = null)
    {
        int vertexSizeInByte = overrideSizeInByte ?? vertex.Length;

        bool needRecreateResource = 
            mesh.VertexBufferResource is null 
         || vertexSizeInByte > mesh.VertexSizeInByte;

        if (needRecreateResource)
        {
            mesh.VertexSizeInByte = vertexSizeInByte;

            GraphicsContext.GraphicsDevice.DestroyResource(mesh.VertexBufferResource);

            mesh.VertexBufferResource = GraphicsContext.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer((ulong)vertexSizeInByte),
                ResourceStates.CopyDest);

            mesh.VertexBufferState = ResourceStates.CopyDest;
        }
        else if (mesh.VertexBufferState != ResourceStates.CopyDest)
        {
            // Transition to CopyDest state
            GraphicsContext.CommandList.ResourceBarrierTransition(
                mesh.VertexBufferResource, mesh.VertexBufferState, ResourceStates.CopyDest);
            mesh.VertexBufferState = ResourceStates.CopyDest;
        }

        Upload(vertex, out var offset);

        GraphicsContext.CommandList.CopyBufferRegion(
            mesh.VertexBufferResource, 0, Resource, (ulong)offset, (ulong)vertexSizeInByte);

        // Transition to GenericRead state
        GraphicsContext.CommandList.ResourceBarrierTransition(
            mesh.VertexBufferResource, ResourceStates.CopyDest, ResourceStates.GenericRead);
        mesh.VertexBufferState = ResourceStates.GenericRead;
    }
}