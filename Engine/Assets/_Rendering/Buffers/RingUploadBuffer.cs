using System.Collections.Generic;

using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.Buffers;

public sealed class GPUUpload
{
    public Format IndexFormat;

    public int[] IndexData;
    public float[] VertexData;

    public List<byte[]> TextureData;

    public Texture2D Texture2D;
    public MeshData MeshData;
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

        UploadBuffer(Kernel.Instance.Context.GraphicsContext.CommandList, ref mesh.IndexBufferResource, ref mesh.IndexBufferState, index, indexSizeInByte, out _);
    }

    public void UploadVertexBuffer(MeshData mesh, Span<byte> vertex, uint? overrideSizeInByte = null)
    {
        uint vertexSizeInByte = overrideSizeInByte ?? (uint)vertex.Length;
        mesh.VertexSizeInByte = vertexSizeInByte;

        UploadBuffer(Kernel.Instance.Context.GraphicsContext.CommandList, ref mesh.VertexBufferResource, ref mesh.VertexBufferState, vertex, vertexSizeInByte, out _);
    }

    public void UploadBuffer(ID3D12GraphicsCommandList5 commandList, ref ID3D12Resource resource, ref ResourceStates resourceState, Span<byte> data, uint sizeInBytes, out uint offset)
    {
        if (resource is null || sizeInBytes > resource.Description.Width)
        {
            // Destroy old resource if it exists
            Kernel.Instance.Context.GraphicsDevice.DestroyResource(resource);

            // Create new resource
            resource = Kernel.Instance.Context.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                ResourceDescription.Buffer(sizeInBytes),
                ResourceStates.CopyDest);

            resourceState = ResourceStates.CopyDest;
        }
        else if (resourceState != ResourceStates.CopyDest)
        {
            // Transition to CopyDest state
            commandList.ResourceBarrierTransition(
                resource, resourceState, ResourceStates.CopyDest);
            resourceState = ResourceStates.CopyDest;
        }

        // Upload data
        Upload(data, out offset);

        // Copy data from the upload buffer to the GPU resource
        commandList.CopyBufferRegion(resource, 0, Resource, offset, sizeInBytes);

        // Transition to GenericRead state
        commandList.ResourceBarrierTransition(resource, ResourceStates.CopyDest, ResourceStates.GenericRead);
        resourceState = ResourceStates.GenericRead;
    }

    public void UploadTexture(ID3D12GraphicsCommandList5 commandList, Texture2D texture2D, List<byte[]> mipData, PlacedSubresourceFootPrint[] layouts, uint[] rowCounts, ulong[] rowSizesInBytes)
    {
        // Create or reuse the texture resource
        if (texture2D.Resource is null
         || texture2D.Width != layouts[0].Footprint.Width
         || texture2D.Height != layouts[0].Footprint.Height)
        {
            var textureDescription = ResourceDescription.Texture2D(
                texture2D.Format,
                texture2D.Width,
                texture2D.Height,
                arraySize: 1,
                mipLevels: (ushort)texture2D.MipLevels);

            if (texture2D.AllowUnorderedAccess)
                textureDescription.Flags = ResourceFlags.AllowUnorderedAccess;

            texture2D.Resource = Kernel.Instance.Context.GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
                HeapProperties.DefaultHeapProperties,
                HeapFlags.None,
                textureDescription,
                ResourceStates.CopyDest);

            texture2D.ResourceStates = ResourceStates.CopyDest;
        }
        else if (texture2D.ResourceStates != ResourceStates.CopyDest)
        {
            commandList.ResourceBarrierTransition(texture2D.Resource, texture2D.ResourceStates, ResourceStates.CopyDest);
            texture2D.ResourceStates = ResourceStates.CopyDest;
        }

        // Upload data
        int totalSize = (int)(layouts[mipData.Count - 1].Offset + layouts[mipData.Count - 1].Footprint.RowPitch * rowCounts[mipData.Count - 1]);
        UploadData(defaultAllignment: false, totalSize, out var mappedData, out var offset);
        CopyMipDataToMappedData(new(mappedData, totalSize), mipData, layouts, rowCounts, rowSizesInBytes);

        // Copy from upload buffer to texture resource
        for (uint i = 0; i < texture2D.MipLevels; i++)
        {
            TextureCopyLocation destination = new(texture2D.Resource, i);

            // Adjust the source location to include the uploadBufferOffset
            PlacedSubresourceFootPrint adjustedLayout = layouts[i];
            adjustedLayout.Offset += offset;

            TextureCopyLocation source = new(Resource, adjustedLayout);

            commandList.CopyTextureRegion(destination, 0, 0, 0, source, null);
        }

        // Transition to PixelShaderResource state
        commandList.ResourceBarrierTransition(texture2D.Resource, ResourceStates.CopyDest, ResourceStates.PixelShaderResource);
        texture2D.ResourceStates = ResourceStates.PixelShaderResource;
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