namespace Engine.Data;

public struct Vertex
{
    public Vector3 Position;
    public Vector2 TexCoord;
    public Vector3 Normal;
    public Vector3 Tangent;
}

public struct MeshInfo
{
    public Vertex[] Vertices;
    public ushort[] Indices;
}
