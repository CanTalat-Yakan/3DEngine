using System.Collections.Generic;

using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.DataTypes;

public sealed class VertexBuffer : IDisposable
{
    public ID3D12Resource Resource;

    public int Offset;
    public int SizeInByte;
    public int Stride;

    public void Dispose()
    {
        if (Offset == 0)
            Resource.Dispose();
    }
}

public sealed class MeshInfo : IDisposable
{
    public ID3D12Resource VertexBufferResource;
    public ID3D12Resource IndexBufferResource;

    public InputLayoutDescription InputLayoutDescription;

    public Dictionary<string, VertexBuffer> Vertices = new();

    public int IndexCount;
    public int IndexSizeInByte;

    public string Name;
    public Format IndexFormat;

    public BoundingBox BoundingBox;

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