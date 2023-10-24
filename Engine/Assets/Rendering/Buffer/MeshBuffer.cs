using System.Runtime.CompilerServices;
using Vortice.Direct3D12;

namespace Engine.Rendering;

public sealed class MeshBuffer
{
    public VertexBufferView VertexBufferView;
    private ID3D12Resource _vertexBuffer;

    public IndexBufferView IndexBufferView;
    private ID3D12Resource _indexBuffer;

    private Renderer _renderer => Renderer.Instance;

    public void CreateBuffer(MeshInfo meshInfo)
    {
        Dispose();

        SetVertexBuffer(meshInfo);
        SetIndexBuffer(meshInfo);
    }

    private void SetVertexBuffer(MeshInfo meshInfo)
    {
        int vertexStride = Unsafe.SizeOf<Vertex>();
        int vertexBufferSize = meshInfo.Vertices.Length * vertexStride;


        //Create a VertexBuffer using the MeshInfos Vertices
        //and bind it with VertexBuffer flag.
        _vertexBuffer = _renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(vertexBufferSize),
            ResourceStates.GenericRead);

        _vertexBuffer.SetData(meshInfo.Vertices);
        VertexBufferView = new VertexBufferView(_vertexBuffer.GPUVirtualAddress, vertexBufferSize, vertexStride);
    }

    private void SetIndexBuffer(MeshInfo meshInfo)
    {
        int indexStride = Unsafe.SizeOf<ushort>();
        int indexBufferSize = meshInfo.Indices.Length * indexStride;

        //Create an IndexBuffer using the MeshInfos Indices
        //and bind it with IndexBuffer flag.
        _indexBuffer = _renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(indexBufferSize),
            ResourceStates.GenericRead);

        _indexBuffer.SetData(meshInfo.Indices);
        IndexBufferView = new(_indexBuffer.GPUVirtualAddress, indexBufferSize, false); // ushort == 16 bits.
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
    }
}