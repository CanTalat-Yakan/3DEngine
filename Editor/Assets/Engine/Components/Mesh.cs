using System.IO;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace Engine.Components;

public class Mesh : Component
{
    public string MeshPath;

    public MeshInfo MeshInfo => _meshInfo;
    [Show] MeshInfo _meshInfo;

    public Material Material => _material;
    [Show] Material _material;

    internal ID3D11Buffer _vertexBuffer;
    internal ID3D11Buffer _indexBuffer;

    internal int _vertexCount => _meshInfo.Vertices.Length;
    internal int _vertexStride => Unsafe.SizeOf<Vertex>();
    internal int _indexCount => _meshInfo.Indices.Length;
    internal int _indexStride => Unsafe.SizeOf<int>();

    private Renderer _d3d => Renderer.Instance;

    public override void OnRegister() =>
        // Register the component with the MeshSystem.
        MeshSystem.Register(this);

    public Mesh()
    {
        SetMeshInfo(EntityManager.GetDefaultMeshInfo());
        SetMaterial(EntityManager.GetDefaultMaterial());
    }

    public override void OnUpdate()
    {
        if (!string.IsNullOrEmpty(MeshPath))
            if (File.Exists(MeshPath))
                try
                {
                    SetMeshInfo(ModelLoader.LoadFile(MeshPath, false));
                }
                finally
                {
                    MeshPath = null;
                }
    }

    public override void OnRender()
    {
        // Set the material's constant buffer to the entity's transform constant buffer.
        _material.Set(Entity.Transform.GetConstantBuffer());

        // Draw the mesh using the Direct3D context.
        _d3d.Draw(
            _vertexBuffer, _vertexStride,
            _indexBuffer, _indexCount);

        // Increment the vertex, index and draw call count in the profiler.
        Profiler.Vertices += _vertexCount;
        Profiler.Indices += _indexCount;
        Profiler.DrawCalls++;
    }

    public void SetMeshInfo(MeshInfo meshInfo)
    {
        // Assign to local variable.
        _meshInfo = meshInfo;

        // Call the "CreateBuffer" method to initialize the vertex and index buffer.
        CreateBuffer();
    }

    public void SetMaterial(Material material) =>
        // Assign to local variable.
        _material = material;

    private void CreateBuffer()
    {
        //Create a VertexBuffer using the MeshInfo's vertices
        //and bind it with VertexBuffer flag.
        _vertexBuffer = _d3d.Device.CreateBuffer(
            _meshInfo.Vertices,
            BindFlags.VertexBuffer);

        //Create an IndexBuffer using the MeshInfo's indices
        //and bind it with IndexBuffer flag.
        _indexBuffer = _d3d.Device.CreateBuffer(
            _meshInfo.Indices,
            BindFlags.IndexBuffer);
    }
}
