using System.Collections.Generic;
using System.IO;

using Engine.RuntimeServices;

namespace Engine.Components;

public sealed class Mesh : Component
{
    public string MeshPath;
    public string MaterialPath;

    public static MeshInfo? CurrentMeshOnGPU { get; set; }
    public static List<MeshInfo> BatchLookup { get; private set; } = new();

    public MeshBuffer MeshBuffers { get; private set; } = new();

    public MeshInfo MeshInfo => _meshInfo;
    [Show] private MeshInfo _meshInfo;

    public Material Material => _material;
    [Show] private Material _material;

    private Renderer _renderer => Renderer.Instance;

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
                try { SetMeshInfo(Loader.ModelLoader.LoadFile(MeshPath, false)); }
                finally { MeshPath = null; }

        if (!string.IsNullOrEmpty(MaterialPath))
            if (File.Exists(MaterialPath))
                try { Output.Log("Set the Material to the Shader " + (_material = MaterialCompiler.MaterialCollector.GetMaterial(new FileInfo(MaterialPath).Name).Material).MaterialBuffer.ShaderName); }
                finally { MaterialPath = null; }
    }

    public override void OnRender()
    {
        if (Equals(Material.CurrentMaterialOnGPU, Material)
            && Equals(Mesh.CurrentMeshOnGPU, MeshInfo))
        {
            // Update the PerModelConstantBuffer only.
            Material.UpdateModelConstantBuffer(Entity.Transform.GetConstantBuffer());

            // Draw the mesh directly without resetting the RenderState.
            _renderer.DrawIndexed(MeshInfo.Indices.Length);
        }
        else
        {
            // Setup the Material, the PerModelConstantBuffer and the PropertiesConstantBuffer.
            Material.Setup();
            Material.UpdateModelConstantBuffer(Entity.Transform.GetConstantBuffer());
            Material.MaterialBuffer?.UpdateConstantBuffer();

            // Draw the mesh with TriangleList.
            _renderer.Data.SetPrimitiveTopology();
            _renderer.Draw(MeshBuffers.VertexBuffer, MeshBuffers.IndexBuffer, MeshInfo.Indices.Length);

            // Assign MeshInfo to the static variable.
            CurrentMeshOnGPU = MeshInfo;
        }

        // Increment the vertex, index and draw call count in the Profiler.
        Profiler.Vertices += MeshInfo.Vertices.Length;
        Profiler.Indices += MeshInfo.Indices.Length;
        Profiler.DrawCalls++;
    }

    public override void OnDestroy()
    {
        MeshBuffers.Dispose();
        Material.Dispose();
    }

    public void SetMeshInfo(MeshInfo meshInfo)
    {
        // Batch MeshInfo by sorting the List with the Order of the Component
        if (BatchLookup.Contains(meshInfo))
        {
            for (int i = 0; i < BatchLookup.Count; i++)
                if (Equals(BatchLookup[i], meshInfo))
                    Order = (byte)i;
        }
        else
        {
            Order = (byte)BatchLookup.Count;
            BatchLookup.Add(meshInfo);
        }

        // Assign to local variable.
        _meshInfo = meshInfo;

        // Call the "CreateBuffer" method to initialize the vertex and index buffer.
        MeshBuffers.CreateBuffer(MeshInfo);
    }

    public void SetMaterial(Material material) =>
        // Assign to local variable.
        _material = material;
}
