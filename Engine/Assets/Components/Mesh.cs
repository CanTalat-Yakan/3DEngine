using System.Collections.Generic;
using System.IO;
using System.Linq;

using Vortice.Mathematics;

namespace Engine.Components;

public sealed class Mesh : Component
{
    public string MeshPath;
    public string MaterialPath;

    public static MeshInfo? CurrentMeshOnGPU { get; set; }
    public static List<MeshInfo> BatchLookup { get; private set; } = new();

    public MeshBuffer MeshBuffers { get; private set; } = new();
    public BoundingBox BoundingBox { get; private set; }

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
                try { Output.Log("Set the Material to " + SetMaterial(new FileInfo(MaterialPath).Name).FileInfo.Name); }
                finally { MaterialPath = null; }
    }

    public override void OnRender()
    {
        BoundingBox.Transform(BoundingBox, Entity.Transform.WorldMatrix, out var transformedBoundingBox);
        if(!Camera.CurrentRenderingCamera.BoundingFrustum.Intersects(transformedBoundingBox))
            return;

        if (Equals(Material.CurrentMaterialOnGPU, Material)
            && Equals(Mesh.CurrentMeshOnGPU, MeshInfo))
        {
            // Update the PerModelConstantBuffer only.
            Material.MaterialBuffer?.UpdateModelConstantBuffer(Entity.Transform.GetConstantBuffer());

            // Draw the mesh directly without resetting the RenderState.
            _renderer.DrawIndexed(MeshInfo.Indices.Length);
        }
        else
        {
            // Setup the Material, the PerModelConstantBuffer and the PropertiesConstantBuffer.
            Material.Setup();
            Material.MaterialBuffer?.UpdateModelConstantBuffer(Entity.Transform.GetConstantBuffer());
            Material.MaterialBuffer?.UpdatePropertiesConstantBuffer();

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
            Order = (byte)BatchLookup.IndexOf(meshInfo);
        else
        {
            Order = (byte)BatchLookup.Count;
            BatchLookup.Add(meshInfo);
        }

        BoundingBox = BoundingBox.CreateFromPoints(meshInfo.Vertices.Select(Vertex => Vertex.Position).ToArray());

        // Assign to local variable.
        _meshInfo = meshInfo;

        // Call the "CreateBuffer" method to initialize the vertex and index buffer.
        MeshBuffers.CreateBuffer(MeshInfo);
    }

    public MaterialEntry SetMaterial(string materialName)
    {
        var MaterialEntry = MaterialCompiler.MaterialLibrary.GetMaterial(materialName);

        SetMaterial(MaterialEntry.Material);

        return MaterialEntry;
    }

    public void SetMaterial(Material material) =>
        // Assign to local variable.
        _material = material;
}
