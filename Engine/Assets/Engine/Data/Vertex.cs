using System.Numerics;

namespace Engine.Data
{
    struct Vertex
    {
        public Vector3 Pos;
        public Vector2 TexCoord;
        public Vector3 Normal;

        public Vertex(
            float x, float y, float z,
            float u, float v,
            float nx, float ny, float nz)
        {
            Pos = new Vector3(x, y, z);
            TexCoord = new Vector2(u, v);
            Normal = new Vector3(nx, ny, nz);
        }
        public Vertex(
            Vector3 _pos,
            Vector2 _tex,
            Vector3 _nor)
        {
            Pos = _pos;
            TexCoord = _tex;
            Normal = _nor;
        }
    }
}
