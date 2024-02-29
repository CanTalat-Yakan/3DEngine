using Vortice.DXGI;

namespace Engine.Buffer;

public sealed class GPUUpload
{
    public float[] VertexData;
    public int[] IndexData;

    public byte[] TextureData;

    public string Name;

    public Format Format;
    public int Stride;

    public Texture2D Texture2D;

    public MeshInfo MeshInfo;
}