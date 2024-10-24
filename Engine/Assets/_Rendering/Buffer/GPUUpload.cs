using System.Collections.Generic;

using Vortice.Direct3D12;
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

public class UploadBuffer : IDisposable
{
    public ID3D12Resource Resource;
    public int Size;

    public void Dispose()
    {
        Resource?.Dispose();

        GC.SuppressFinalize(this);
    }
}
