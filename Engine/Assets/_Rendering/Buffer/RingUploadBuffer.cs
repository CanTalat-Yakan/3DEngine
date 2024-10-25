using System.Collections.Generic;
using System.Runtime.CompilerServices;

using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.Buffer;

public unsafe sealed partial class RingUploadBuffer : UploadBuffer
{
    private IntPtr CPUResourcePointer;
    private ulong GPUResourcePointer;

    private int AllocateIndex = 0;

    private const int DefaultAlignment = 256;
    private const int TextureAlignment = D3D12.TextureDataPlacementAlignment;

    public GraphicsContext GraphicsContext => _graphicsContext ??= Kernel.Instance.Context.GraphicsContext;
    private GraphicsContext _graphicsContext;

    public void Initialize(GraphicsDevice device, int size)
    {
        Size = size;
        device.CreateUploadBuffer(this, size);

        void* pointer = null;
        Resource.Map(0, null, &pointer).CheckError();

        CPUResourcePointer = new IntPtr(pointer);
        GPUResourcePointer = Resource.GPUVirtualAddress;
    }

    public void Upload<T>(Span<T> data, out uint offset) where T : struct
    {
        UploadData(defaultAllignment: true, data.Length * Unsafe.SizeOf<T>(), out var mappedData, out offset);
        data.CopyTo(new Span<T>(mappedData, data.Length));
    }

    public void Upload<T>(T data, out uint offset)
    {
        UploadData(defaultAllignment: true, Unsafe.SizeOf<T>(), out var mappedData, out offset);
        Unsafe.Copy(mappedData, ref data);
    }

    public void UploadData(bool defaultAllignment, int size, out void* mappedData, out uint offset)
    {
        if (defaultAllignment)
        {
            if (!AllocateUploadMemory(size, DefaultAlignment, out offset))
                throw new InvalidOperationException("Not enough space in the RingUploadBuffer.");
        }
        else
        {
            if (!AllocateUploadMemory(AlignUp(size, TextureAlignment), TextureAlignment, out offset))
                throw new InvalidOperationException("Not enough space in the RingUploadBuffer.");
        }

        mappedData = (CPUResourcePointer + (int)offset).ToPointer();
    }
}

public unsafe sealed partial class RingUploadBuffer : UploadBuffer
{
    public void UploadIndexBuffer(MeshData mesh, Span<byte> index, Format indexFormat, uint? overrideSizeInByte = null)
    {
        uint indexSizeInByte = overrideSizeInByte ?? (uint)index.Length;
        uint indexCount = indexSizeInByte / (uint)GraphicsDevice.GetSizeInByte(indexFormat);

        mesh.IndexFormat = indexFormat;
        mesh.IndexSizeInByte = indexSizeInByte;
        mesh.IndexCount = indexCount;

        UploadBuffer(ref mesh.IndexBufferResource, ref mesh.IndexBufferState, index, indexSizeInByte, out _);
    }

    public void UploadVertexBuffer(MeshData mesh, Span<byte> vertex, uint? overrideSizeInByte = null)
    {
        uint vertexSizeInByte = overrideSizeInByte ?? (uint)vertex.Length;
        mesh.VertexSizeInByte = vertexSizeInByte;

        UploadBuffer(ref mesh.VertexBufferResource, ref mesh.VertexBufferState, vertex, vertexSizeInByte, out _);
    }

    public void UploadBuffer(ref ID3D12Resource resource, ref ResourceStates resourceState, Span<byte> data, uint sizeInBytes, out uint offset)
    {
        if (resource is null || sizeInBytes > resource.Description.Width)
        {
            // Destroy old resource if it exists
            GraphicsContext.GraphicsDevice.DestroyResource(resource);

            // Create new resource
            resource = GraphicsContext.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer(sizeInBytes),
                ResourceStates.CopyDest);

            resourceState = ResourceStates.CopyDest;
        }
        else if (resourceState != ResourceStates.CopyDest)
        {
            // Transition to CopyDest state
            GraphicsContext.CommandList.ResourceBarrierTransition(
                resource, resourceState, ResourceStates.CopyDest);
            resourceState = ResourceStates.CopyDest;
        }

        // Upload data
        Upload(data, out offset);

        // Copy data from the upload buffer to the GPU resource
        GraphicsContext.CommandList.CopyBufferRegion(resource, 0, Resource, offset, sizeInBytes);

        // Transition to GenericRead state
        GraphicsContext.CommandList.ResourceBarrierTransition(resource, ResourceStates.CopyDest, ResourceStates.GenericRead);
        resourceState = ResourceStates.GenericRead;
    }

    public void UploadTexture(Texture2D texture, List<byte[]> mipData, PlacedSubresourceFootPrint[] layouts, uint[] rowCounts, ulong[] rowSizesInBytes)
    {
        // Create or reuse the texture resource
        if (texture.Resource is null
         || texture.Width != layouts[0].Footprint.Width
         || texture.Height != layouts[0].Footprint.Height)
        {
            var textureDescription = ResourceDescription.Texture2D(
                texture.Format,
                texture.Width,
                texture.Height,
                arraySize: 1,
                mipLevels: (ushort)texture.MipLevels);

            if (texture.AllowUnorderedAccess)
                textureDescription.Flags = ResourceFlags.AllowUnorderedAccess;

            texture.Resource = GraphicsContext.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None, 
                textureDescription,
                ResourceStates.CopyDest);

            texture.ResourceStates = ResourceStates.CopyDest;
        }
        else if (texture.ResourceStates != ResourceStates.CopyDest)
        {
            GraphicsContext.CommandList.ResourceBarrierTransition(texture.Resource, texture.ResourceStates, ResourceStates.CopyDest);
            texture.ResourceStates = ResourceStates.CopyDest;
        }

        // Upload data
        int totalSize = (int)(layouts[mipData.Count - 1].Offset + layouts[mipData.Count - 1].Footprint.RowPitch * rowCounts[mipData.Count - 1]);
        UploadData(defaultAllignment: false, totalSize, out var mappedData, out var offset);
        CopyMipDataToMappedData(new(mappedData, totalSize), mipData, layouts, rowCounts, rowSizesInBytes);

        // Copy from upload buffer to texture resource
        for (uint i = 0; i < texture.MipLevels; i++)
        {
            TextureCopyLocation destination = new(texture.Resource, i);

            // Adjust the source location to include the uploadBufferOffset
            PlacedSubresourceFootPrint adjustedLayout = layouts[i];
            adjustedLayout.Offset += offset;

            TextureCopyLocation source = new(Resource, adjustedLayout);

            GraphicsContext.CommandList.CopyTextureRegion(destination, 0, 0, 0, source, null);
        }

        // Transition to PixelShaderResource state
        GraphicsContext.CommandList.ResourceBarrierTransition(texture.Resource, ResourceStates.CopyDest, ResourceStates.PixelShaderResource);
        texture.ResourceStates = ResourceStates.PixelShaderResource;
    }

    private void CopyMipDataToMappedData(Span<byte> mappedData, List<byte[]> mipData, PlacedSubresourceFootPrint[] layouts, uint[] rowCounts, ulong[] rowSizesInBytes)
    {
        for (int i = 0; i < mipData.Count; i++)
        {
            var footprint = layouts[i];
            ulong subresourceOffset = footprint.Offset;
            uint rowCount = rowCounts[i];
            ulong rowSizeInBytes = rowSizesInBytes[i];
            uint rowPitch = footprint.Footprint.RowPitch;

            byte[] sourceData = mipData[i];
            uint sourceRowPitch = (uint)(sourceData.Length / rowCount);

            for (uint row = 0; row < rowCount; row++)
            {
                int destiantionOffset = (int)(subresourceOffset + row * rowPitch);
                int sourceOffset = (int)(row * sourceRowPitch);
                int copySize = (int)MathF.Min(rowSizeInBytes, sourceData.Length - sourceOffset);

                sourceData.AsSpan(sourceOffset, copySize).CopyTo(mappedData.Slice(destiantionOffset, copySize));
            }
        }
    }

}

public unsafe sealed partial class RingUploadBuffer : UploadBuffer
{
    private bool AllocateUploadMemory(int size, int alignment, out uint offset)
    {
        int alignedSize = AlignUp(size, alignment);
        int alignedAllocateIndex = AlignUp(AllocateIndex, alignment);

        if (alignedAllocateIndex + alignedSize > Size)
        {
            // Wrap around if not enough space
            AllocateIndex = 0;
            alignedAllocateIndex = AlignUp(AllocateIndex, alignment);

            if (alignedAllocateIndex + alignedSize > Size)
            {
                offset = 0;
                return false; // Not enough space even after wrap-around
            }
        }

        offset = (uint)alignedAllocateIndex;
        AllocateIndex = (alignedAllocateIndex + alignedSize) % Size;
        return true;
    }

    private int AlignUp(int value, int alignment) =>
        (value + alignment - 1) & ~(alignment - 1);
}