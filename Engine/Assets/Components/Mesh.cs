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
    public bool InBounds { get; set; }

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public GraphicsContext GraphicsContext => _graphicsContext ??= Kernel.Instance.Context.GraphicsContext;
    public GraphicsContext _graphicsContext;

    public override void OnRegister() =>
        MeshSystem.Register(this);

    public override void OnUpdate()
    {
        if (MeshInfo is null)
            return;

        if (!EditorState.EditorBuild)
            CheckBounds();
    }

    public override void OnRender()
    {
        if (MeshInfo is null || Material is null)
            return;

        if (EditorState.EditorBuild)
            CheckBounds();

        if (!InBounds)
            return;
        
        //if (MeshInfo.Equals(CurrentMeshInfoOnGPU)
        // && Material.Equals(CurrentMaterialOnGPU))
        //    Context.GraphicsContext.DrawIndexedInstanced(MeshInfo.IndexCount, 1, 0, 0, 0);
        //else
        {
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

        InstantiateBounds(meshInfo.BoundingBox);

        Material.SetRootSignature("CC");
    }

    public void SetMaterialTexture(params MaterialTextureEntry[] textureEntries)
    {
        Material.MaterialTextures.AddRange(textureEntries);

        StringBuilder stringBuilder = new();
        for (int i = 0; i < textureEntries.Length; i++)
            stringBuilder.Append("s");
        var shaderResourceViews = stringBuilder.ToString();

        Material.SetRootSignature("CC" + shaderResourceViews);
    }

    private void InstantiateBounds(BoundingBox boundingBox)
    {
        MeshInfo.BoundingBox = boundingBox;

        TransformedBoundingBox = BoundingBox.Transform(
            MeshInfo.BoundingBox,
            Entity.Transform.WorldMatrix);
    }

    private void CheckBounds()
    {
        if (Entity.Transform.TransformChanged)
            TransformedBoundingBox = BoundingBox.Transform(
                MeshInfo.BoundingBox,
                Entity.Transform.WorldMatrix);

        if (Entity.Transform.TransformChanged || (Camera.CurrentRenderingCamera?.Entity.Transform.TransformChanged ?? false))
        {
            var boundingFrustum = Camera.CurrentRenderingCamera.BoundingFrustum;
            if (boundingFrustum is not null)
                InBounds = boundingFrustum.Value.Intersects(TransformedBoundingBox);
        }
    }
}