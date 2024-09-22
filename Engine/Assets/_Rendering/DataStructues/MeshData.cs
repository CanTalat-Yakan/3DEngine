using System.Collections.Generic;

using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.DataStructures;

public record Vertex(Vector3 position, Vector3 normal, Vector3 tangent, Vector2 uv);

public sealed class VertexBuffer : IDisposable
{
    public ID3D12Resource Resource;

    public int Offset;
    public int SizeInByte;
    public int Stride;

    public void Dispose()
    {
        if (Offset == 0)
            Resource?.Dispose();
    }
}

public sealed class MeshData : IDisposable
{
    public string Name;

    public ID3D12Resource IndexBufferResource;
    public ID3D12Resource VertexBufferResource;

    public InputLayoutDescription InputLayoutDescription;

    public Dictionary<string, VertexBuffer> Vertices = new();

    public Format IndexFormat;

    public int IndexCount;
    public int IndexSizeInByte;
    public int IndexStride;

    public int VertexCount;
    public int VertexSizeInByte;
    public int VertexStride;

    public BoundingBox BoundingBox;

    public DateTime LastTimeUsed;

    public void SetVertexBuffers() =>
        Vertices.SetVertexBuffer(VertexBufferResource, VertexSizeInByte);

    public bool IsValid() =>
        VertexBufferResource is not null && VertexStride != 0;

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
    }
}