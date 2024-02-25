using Vortice.DXGI;

namespace Engine.Buffer;

public sealed class GPUUpload
{
    public byte[] VertexData;
    public byte[] IndexData;
    public byte[] TextureData;

    public string Name;
    public Format Format;
    public int Stride;

    public Texture2D Texture2D;
}