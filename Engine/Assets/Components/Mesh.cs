using System.Collections.Generic;
using System.Text;

using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Components;

public struct MaterialTextureEntry(string name, int slot)
{
    public string Name = name;
    public int Slot = slot;
}

public sealed partial class Mesh : EditorComponent
{
    public List<MaterialTextureEntry> MaterialTextures { get; private set; } = new();
    public MeshInfo MeshInfo { get; private set; }
    public RootSignature RootSignature { get; private set; }

    public BoundingBox TransformedBoundingBox { get; private set; }
    public bool InBounds { get; set; }

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public GraphicsContext GraphicsContext => _graphicsContext ??= Kernel.Instance.Context.GraphicsContext;
    public GraphicsContext _graphicsContext;

    public PipelineStateObjectDescription PipelineStateObjectDescription = new()
    {
        InputLayout = "SimpleLit",
        CullMode = CullMode.None,
        RenderTargetFormat = Format.R8G8B8A8_UNorm,
        RenderTargetCount = 1,
        PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
        BlendState = "Alpha",
    };

    public override void OnRegister() =>
        MeshSystem.Register(this);

    public override void OnAwake()
    {

    }

    public override void OnUpdate()
    {
        if (MeshInfo is null)
            return;

        if (!EditorState.EditorBuild)
            CheckBounds();
    }

    public override void OnRender()
    {
        if (MeshInfo is null)
            return;

        if (EditorState.EditorBuild)
            CheckBounds();

        //if (InBounds)
        {
            Context.GraphicsContext.SetPipelineState(Context.PipelineStateObjects["SimpleLit"], PipelineStateObjectDescription);
            Context.GraphicsContext.SetRootSignature(RootSignature);
            Context.GraphicsContext.SetMesh(MeshInfo);

            foreach (var texture in MaterialTextures)
                Context.GraphicsContext.SetShaderResourceView(Context.GetTextureByString(texture.Name), texture.Slot);

            Context.UploadBuffer.Upload(Entity.Transform.GetConstantBuffer(), out var offset);
            Context.UploadBuffer.SetConstantBufferView(offset, 1);

            Context.GraphicsContext.DrawIndexedInstanced(MeshInfo.IndexCount, 1, 0, 0, 0);
        }

        Profiler.Vertices += MeshInfo.VertexCount;
        Profiler.Indices += MeshInfo.IndexCount;
        Profiler.DrawCalls++;
    }

    public override void OnDestroy()
    {
        MeshInfo?.Dispose();
        RootSignature?.Dispose();
    }
}

public sealed partial class Mesh : EditorComponent
{
    public void SetMeshInfo(MeshInfo meshInfo)
    {
        MeshInfo = meshInfo;

        InstantiateBounds(meshInfo.BoundingBox);

        RootSignature?.Dispose();
        RootSignature = Context.CreateRootSignatureFromString("CC");
    }

    public void SetMaterialTexture(params MaterialTextureEntry[] textureEntries)
    {
        MaterialTextures.AddRange(textureEntries);

        StringBuilder stringBuilder = new();
        for (int i = 0; i < textureEntries.Length; i++)
            stringBuilder.Append("s");

        var shaderResourceViews = stringBuilder.ToString();

        RootSignature?.Dispose();
        RootSignature = Context.CreateRootSignatureFromString("CC" + shaderResourceViews);
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