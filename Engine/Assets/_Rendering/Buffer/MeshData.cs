using System.Collections.Generic;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Buffer;

public record Vertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 uv);

public sealed class VertexBuffer : IDisposable
{
    public ID3D12Resource Resource;

    public uint Offset;
    public uint SizeInByte;
    public uint Stride;

    public void Dispose()
    {
        if (Offset == 0)
        {
            Resource?.Dispose();
            Resource = null;
        }

        GC.SuppressFinalize(this);
    }
}

public sealed class MeshData : IDisposable
{
    public string Name;

    public ID3D12Resource VertexBufferResource;
    public ID3D12Resource IndexBufferResource;

    public ResourceStates VertexBufferState = ResourceStates.GenericRead;
    public ResourceStates IndexBufferState = ResourceStates.GenericRead;

    public InputLayoutDescription InputLayoutDescription;

    public Dictionary<string, VertexBuffer> Vertices = new();

    public uint VertexCount;
    public uint VertexSizeInByte;
    public uint VertexStride;

    public uint IndexCount;
    public uint IndexSizeInByte;
    public uint IndexStride;

    public Format IndexFormat;

    public BoundingBox BoundingBox;

    public DateTime LastTimeUsed;

    public void SetVertexBuffers() =>
        Vertices.SetVertexBuffer(VertexBufferResource, VertexSizeInByte);

    public bool IsValid() =>
        VertexBufferResource is not null
     && VertexBufferState is ResourceStates.GenericRead;

    public void Dispose()
    {
        IndexBufferResource?.Dispose();
        IndexBufferResource = null;

        VertexBufferResource?.Dispose();
        VertexBufferResource = null;

        if (Vertices is not null)
            foreach (var pair in Vertices)
                pair.Value.Dispose();

        Vertices?.Clear();

        GC.SuppressFinalize(this);
    }
}