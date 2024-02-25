using Vortice.Mathematics;

namespace Engine.Data;

public struct Vertex
{
    public Vector3 Position;
    public Vector3 Normal;
    public Vector3 Tangent;
    public Vector2 TextureCoordinate;
}

public struct MeshInfo_OLD
{
    public Vertex[] Vertices;
    public int[] Indices;

    public BoundingBox BoundingBox;
}