using Engine.DataTypes;
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
}

public unsafe sealed partial class RingUploadBuffer : UploadBuffer
{
    public void SetConstantBufferView(int offset, int slot) =>
        GraphicsContext.SetConstantBufferView(this, offset, slot);

    public void UploadMeshIndex(MeshInfo mesh, Span<byte> index, Format indexFormat)
    {
        int uploadOffset = Upload(index);
        if (mesh.IndexFormat != indexFormat
         || mesh.IndexCount != index.Length / (indexFormat == Format.R32_UInt ? 4 : 2)
         || mesh.IndexSizeInByte != index.Length)
        {
            mesh.IndexFormat = indexFormat;
            mesh.IndexCount = index.Length / (indexFormat == Format.R32_UInt ? 4 : 2);
            mesh.IndexSizeInByte = index.Length;

            GraphicsContext.GraphicsDevice.DestroyResource(mesh.IndexBufferResource);
            mesh.IndexBufferResource = GraphicsContext.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer((ulong)index.Length),
                ResourceStates.CopyDest);
        }
        else
            GraphicsContext.CommandList.ResourceBarrierTransition(mesh.IndexBufferResource, ResourceStates.GenericRead, ResourceStates.CopyDest);

        GraphicsContext.CommandList.CopyBufferRegion(mesh.IndexBufferResource, 0, Resource, (ulong)uploadOffset, (ulong)index.Length);
        GraphicsContext.CommandList.ResourceBarrierTransition(mesh.IndexBufferResource, ResourceStates.CopyDest, ResourceStates.GenericRead);
    }

    public void UploadVertexBuffer(MeshInfo meshInfo, Span<byte> vertex)
    {
        GraphicsContext.GraphicsDevice.DestroyResource(meshInfo.VertexBufferResource);
        meshInfo.VertexBufferResource = GraphicsContext.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
            HeapProperties.DefaultHeapProperties,
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)vertex.Length),
            ResourceStates.CopyDest);

        int uploadOffset = Upload(vertex);
        GraphicsContext.CommandList.CopyBufferRegion(meshInfo.VertexBufferResource, 0, Resource, (ulong)uploadOffset, (ulong)vertex.Length);
        GraphicsContext.CommandList.ResourceBarrierTransition(meshInfo.VertexBufferResource, ResourceStates.CopyDest, ResourceStates.GenericRead);
    }
}