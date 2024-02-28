using System.IO;
using System.Linq;

using Vortice.Mathematics;

namespace Engine.Components;

public sealed partial class Mesh : EditorComponent
{
    public string MeshPath;
    public string MaterialPath;

    public static MeshInfo? OnGPU { get; set; }

    public BoundingBox TransformedBoundingBox { get; private set; }
    public bool InBounds { get; set; }

    public MeshInfo ? MeshInfo => _meshInfo;
    [Show] private MeshInfo _meshInfo;

    public Material_OLD Material => _material;
    [Show] private Material_OLD _material;

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public GraphicsContext GraphicsContext => _graphicsContext ??= Kernel.Instance.Context.GraphicsContext;
    public GraphicsContext _graphicsContext;

    public override void OnRegister() =>
        MeshSystem.Register(this);

    public override void OnAwake()
    {
        //SetMeshInfo(EntityManager.GetDefaultMeshInfo());
        //SetMaterial(EntityManager.GetDefaultMaterial());
    }

    public override void OnUpdate()
    {
        //if (!string.IsNullOrEmpty(MeshPath))
        //    if (File.Exists(MeshPath))
        //        try { SetMeshInfo(Loader.ModelLoader.LoadFile(MeshPath, false)); }
        //        finally { MeshPath = null; }

        //if (!string.IsNullOrEmpty(MaterialPath))
        //    if (File.Exists(MaterialPath))
        //        try { Output.Log("Set the Material to " + SetMaterial(new FileInfo(MaterialPath).Name).FileInfo.Name); }
        //        finally { MaterialPath = null; }

        //if (!EditorState.EditorBuild)
        //    CheckBounds();
    }

    public override void OnRender()
    {
        //// With Parallelism the App can't catch up and breaks.
        //// So check bounds in the same thread as the render call.
        //if (EditorState.EditorBuild)
        //    CheckBounds();

        //if (!InBounds)
        //    return;

        if (Equals(Material_OLD.OnGPU, Material)
         && Equals(Mesh.OnGPU, MeshInfo))
        {
            //Material.MaterialBuffer?.UpdateModelConstantBuffer(Entity.Transform.GetConstantBuffer());

            //GraphicsContext.DrawIndexedInstanced(MeshInfo.Value.Indices.Length, MeshBuffers.IndexBufferView, 1, MeshBuffers.VertexBufferView);
        }
        else
        {
            // Setup Material, PerModel and Properties constant buffer.
            //Material.Setup();
            //Material.MaterialBuffer?.UpdateModelConstantBuffer(Entity.Transform.GetConstantBuffer());
            //Material.MaterialBuffer?.UpdatePropertiesConstantBuffer();

            //GraphicsContext.DrawIndexedInstanced(MeshInfo.Value.Indices.Length, MeshBuffers.IndexBufferView, 1, MeshBuffers.VertexBufferView);

            // Assign MeshInfo to the static variable.
            OnGPU = MeshInfo;
        }

        // Increment the vertex, index and draw call count in the Profiler.
        //Profiler.Vertices += MeshInfo.Vertices.Length;
        //Profiler.Indices += MeshInfo.Value.Indices.Length;
        Profiler.DrawCalls++;
    }

    public override void OnDestroy()
    {
        Material.Dispose();
    }
}

public sealed partial class Mesh : EditorComponent
{
    public void SetMeshInfo(MeshInfo meshInfo)
    {
        if (Context.Meshes.ContainsKey(meshInfo.Name))
        {
            //Order = (byte)Context.Meshes.Keys.IndexOf(meshInfo.Name);
            meshInfo = Context.Meshes[meshInfo.Name];
            _meshInfo = meshInfo;

            return;
        }
        else
        {
            Order = (byte)Context.Meshes.Count();
            Context.Meshes[meshInfo.Name] = meshInfo;

            //InstantiateBounds(BoundingBox.CreateFromPoints(meshInfo.Ver))
            //InstantiateBounds(BoundingBox.CreateFromPoints(
            //    meshInfo.Vertices.Select(Vertex => Vertex.Position).ToArray()));
        }

        // Call the "CreateBuffer" method to initialize the vertex and index buffer.
        //MeshBuffers.CreateBuffer(MeshInfo.Value);
    }

    public MaterialEntry SetMaterial(string materialName)
    {
        var MaterialEntry = MaterialCompiler.Library.GetMaterial(materialName);

        SetMaterial(MaterialEntry.Material);

        return MaterialEntry;
    }

    public void SetMaterial(Material_OLD material) =>
        // Assign to local variable.
        _material = material;
}

public sealed partial class Mesh : EditorComponent
{
    private void InstantiateBounds(BoundingBox boundingBox)
    {
        _meshInfo.BoundingBox = boundingBox;

        TransformedBoundingBox = BoundingBox.Transform(
            _meshInfo.BoundingBox, 
            Entity.Transform.WorldMatrix);
    }

    private void CheckBounds()
    {
        if (Entity.Transform.TransformChanged)
            TransformedBoundingBox = BoundingBox.Transform(
                _meshInfo.BoundingBox, 
                Entity.Transform.WorldMatrix);

        if (Entity.Transform.TransformChanged
            || (Camera.CurrentRenderingCamera?.Entity.Transform.TransformChanged ?? false))
        {
            var boundingFrustum = Camera.CurrentRenderingCamera.BoundingFrustum;
            if (boundingFrustum is not null)
                InBounds = boundingFrustum.Value.Intersects(TransformedBoundingBox);
        }
    }
}