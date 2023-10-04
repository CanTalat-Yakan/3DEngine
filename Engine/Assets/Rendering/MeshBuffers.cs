using Vortice.Direct3D11;

namespace Engine.Utilities;

public sealed class MeshBuffers
{
    public ID3D11Buffer VertexBuffer;
    public ID3D11Buffer IndexBuffer;

    private Renderer _renderer => Renderer.Instance;

    public void Dispose()
    {
        VertexBuffer?.Dispose();
        IndexBuffer?.Dispose();
    }

    public void CreateBuffer(MeshInfo meshInfo)
    {
        Dispose();

        //Create a VertexBuffer using the MeshInfo's vertices
        //and bind it with VertexBuffer flag.
        VertexBuffer = _renderer.Device.CreateBuffer(
            meshInfo.Vertices,
            BindFlags.VertexBuffer);

        //Create an IndexBuffer using the MeshInfo's indices
        //and bind it with IndexBuffer flag.
        IndexBuffer = _renderer.Device.CreateBuffer(
            meshInfo.Indices,
            BindFlags.IndexBuffer);
    }
}
