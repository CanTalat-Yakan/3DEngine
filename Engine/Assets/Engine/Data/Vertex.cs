namespace Engine.Data;

public struct Vertex
{
    public Vector3 Position;
    public Vector2 TexCoord;
    public Vector3 Normal;

    public Vertex(
        float x, float y, float z,
        float u, float v,
        float nx, float ny, float nz)
    {
        Position = new(x, y, z);
        TexCoord = new(u, v);
        Normal = new(nx, ny, nz);
    }

    public Vertex(Vector3 position, Vector2 texCoord, Vector3 normal)
    {
        Position = position;
        TexCoord = texCoord;
        Normal = normal;
    }
}
