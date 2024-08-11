using System.IO;
using System.Linq;
using System.Text;

using Vortice.Mathematics;

namespace Engine.Components;

public sealed partial class Mesh : EditorComponent
{
    public static MeshInfo CurrentMeshInfoOnGPU { get; set; }
    public static Material CurrentMaterialOnGPU { get; set; }

    public MeshInfo MeshInfo { get; private set; }
    public Material Material { get; private set; } = new();

    public BoundingBox TransformedBoundingBox { get; private set; }
    public bool InBounds { get; set; } = true;

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public GraphicsContext GraphicsContext => _graphicsContext ??= Kernel.Instance.Context.GraphicsContext;
    public GraphicsContext _graphicsContext;

    public override void OnRegister() =>
        MeshSystem.Register(this);

    public override void OnStart() =>
        CheckBounds();

    public string MeshPath;
    public string ShaderName;
    public override void OnUpdate()
    {
        if (!string.IsNullOrEmpty(MeshPath))
            if (File.Exists(MeshPath))
                try { SetMeshInfo(Loader.ModelLoader.LoadFile(MeshPath)); }
                finally { MeshPath = null; }

        if (!string.IsNullOrEmpty(ShaderName))
            try { SetMaterialPipeline(ShaderName); }
            finally { ShaderName = null; }
    }

    public override void OnFixedRender()
    {
        if (MeshInfo is null || Material is null)
            return;

        if (!InBounds)
            return;

        if (MeshInfo.Equals(CurrentMeshInfoOnGPU)
         && Material.Equals(CurrentMaterialOnGPU))
        {
            Context.UploadBuffer.Upload(Entity.Transform.GetConstantBuffer(), out var offset);
            Context.UploadBuffer.SetConstantBufferView(offset, 1);

            Context.GraphicsContext.DrawIndexedInstanced(MeshInfo.IndexCount, 1, 0, 0, 0);
        }
        else
        {
            Material.PipelineStateObjectDescription.Wireframe = ViewportController.Camera.RenderMode switch
            {
                RenderMode.Wireframe => true,
                RenderMode.Shaded => false,
                _ => false
            };

            Material.Setup();

            Context.GraphicsContext.SetMesh(MeshInfo);

            Context.UploadBuffer.Upload(Entity.Transform.GetConstantBuffer(), out var offset);
            Context.UploadBuffer.SetConstantBufferView(offset, 1);

            Context.GraphicsContext.DrawIndexedInstanced(MeshInfo.IndexCount, 1, 0, 0, 0);

            CurrentMeshInfoOnGPU = MeshInfo;
            CurrentMaterialOnGPU = Material;
        }

        Profiler.Vertices += MeshInfo.VertexCount;
        Profiler.Indices += MeshInfo.IndexCount;
        Profiler.DrawCalls++;
    }

    public override void OnDestroy() =>
        MeshInfo?.Dispose();
}

public sealed partial class Mesh : EditorComponent
{
    public void SetMeshInfo(MeshInfo meshInfo)
    {
        MeshInfo = meshInfo;
        Order = (byte)Array.IndexOf(Assets.Meshes.Values.ToArray(), meshInfo);

        InstantiateBounds(meshInfo.BoundingBox);

        Material.SetRootSignature("CC");
    }

    public void SetMaterialTextures(params MaterialTextureEntry[] textureEntries)
    {
        Material.MaterialTextures.AddRange(textureEntries);

        StringBuilder stringBuilder = new();
        for (int i = 0; i < textureEntries.Length; i++)
            stringBuilder.Append("s");
        var shaderResourceViews = stringBuilder.ToString();

        Material.SetRootSignature("CC" + shaderResourceViews);
    }

    public void SetMaterialPipeline(string pipelineStateObjectName) =>
        Material.SetPipelineStateObject(pipelineStateObjectName);

    private void InstantiateBounds(BoundingBox boundingBox)
    {
        MeshInfo.BoundingBox = boundingBox;

        TransformedBoundingBox = BoundingBox.Transform(
            MeshInfo.BoundingBox,
            Entity.Transform.WorldMatrix);
    }

    private void CheckBounds()
    {
        if (MeshInfo is null)
            return;

        Entity.Transform.TransformChanged += () =>
            TransformedBoundingBox = BoundingBox.Transform(
                MeshInfo.BoundingBox,
                Entity.Transform.WorldMatrix);

        if (Camera.CurrentRenderingCamera is not null)
            Camera.CurrentRenderingCamera.Entity.Transform.TransformChanged += () =>
            {
                var boundingFrustum = Camera.CurrentRenderingCamera.BoundingFrustum;
                if (boundingFrustum is not null)
                    InBounds = boundingFrustum.Value.Intersects(TransformedBoundingBox);
            };
    }
}