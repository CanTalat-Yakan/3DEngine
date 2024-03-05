using Vortice.DXGI;

namespace Engine.Buffer;

public sealed class GPUUpload
{
    public Format IndexFormat;

    public int[] IndexData;
    public float[] VertexData;

    public byte[] TextureData;

    public Texture2D Texture2D;
    public MeshInfo MeshInfo;
}