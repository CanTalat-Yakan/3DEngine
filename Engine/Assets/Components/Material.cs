using System.Collections.Generic;

using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.Components;

public struct MaterialTextureEntry(string name, int slot)
{
    public string Name = name;
    public int Slot = slot;
}

public sealed class Material : EditorComponent, IHide, IEquatable<Material>
{
    public static PipelineStateObject CurrentPipelineStateOnGPU { get; set; }

    public List<MaterialTextureEntry> MaterialTextures { get; private set; } = new();
    public RootSignature RootSignature { get; private set; }

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public PipelineStateObjectDescription PipelineStateObjectDescription = new()
    {
        InputLayout = "SimpleLit",
        CullMode = CullMode.Back,
        RenderTargetFormat = Format.R8G8B8A8_UNorm,
        RenderTargetCount = 1,
        PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
        BlendState = "Alpha",
    };

    public override void OnDestroy() =>
        RootSignature?.Dispose();

    public void Setup()
    {
        Context.GraphicsContext.SetPipelineState(Context.PipelineStateObjects["SimpleLit"], PipelineStateObjectDescription);
        Context.GraphicsContext.SetRootSignature(RootSignature);

        foreach (var texture in MaterialTextures)
            Context.GraphicsContext.SetShaderResourceView(Context.GetTextureByString(texture.Name), texture.Slot);
    }

    public bool Equals(Material other) =>
        RootSignature == other.RootSignature
     && MaterialTextures.Count == other.MaterialTextures.Count;

    public void SetRootSignature(string rootSignatureParameters)
    {
        RootSignature?.Dispose();
        RootSignature = Context.CreateRootSignatureFromString(rootSignatureParameters);
    }
}