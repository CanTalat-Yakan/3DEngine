using System.Collections.Generic;
using Vortice.DXGI;

namespace Engine.Buffer;

public sealed class GPUUpload
{
    public Format IndexFormat;

    public int[] IndexData;
    public float[] VertexData;

    public List<byte[]> TextureData;

    public Texture2D Texture2D;
    public MeshData MeshData;
}