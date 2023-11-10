using System.Runtime.CompilerServices;

using Vortice.Direct3D12;

namespace Engine.Rendering;

public sealed class MeshBuffer
{
    public VertexBufferView VertexBufferView;
    private ID3D12Resource _vertexBuffer;

    public IndexBufferView IndexBufferView;
    private ID3D12Resource _indexBuffer;

    internal Renderer Renderer => _renderer ??= Renderer.Instance;
    private Renderer _renderer;

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
        _vertexBuffer = Renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(vertexBufferSize),
            ResourceStates.GenericRead);
        _vertexBuffer.Name = "VertexBuffer";

        _vertexBuffer.SetData(meshInfo.Vertices);
        VertexBufferView = new VertexBufferView(_vertexBuffer.GPUVirtualAddress, vertexBufferSize, vertexStride);
    }

    private void SetIndexBuffer(MeshInfo meshInfo)
    {
        int indexStride = Unsafe.SizeOf<int>();
        int indexBufferSize = meshInfo.Indices.Length * indexStride;

        //Create an IndexBuffer using the MeshInfos Indices
        //and bind it with IndexBuffer flag.
        _indexBuffer = Renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(indexBufferSize),
            ResourceStates.GenericRead);
        _indexBuffer.Name = "IndexBuffer";

        _indexBuffer.SetData(meshInfo.Indices);
        IndexBufferView = new(_indexBuffer.GPUVirtualAddress, indexBufferSize, true); // 32 bit == int, 16 bits == ushort.
    }

    public void Dispose()
    {
        _vertexBuffer?.Dispose();
        _indexBuffer?.Dispose();
    }
}