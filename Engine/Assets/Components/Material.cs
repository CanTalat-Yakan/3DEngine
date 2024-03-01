using System.Collections.Generic;
using System.Text;

using Vortice.Direct3D12;
using Vortice.DXGI;

namespace Engine.Components;

public struct MaterialTextureEntry(string name, int slot)
{
    public string Name = name;
    public int Slot = slot;
}

public sealed partial class Material : EditorComponent, IHide
{
    public static PipelineStateObject CurrentPipelineStateOnGPU { get; set; }
    public List<MaterialTextureEntry> MaterialTextures { get; private set; } = new();
    public RootSignature RootSignature { get; private set; }

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

    public override void OnDestroy() =>
        RootSignature?.Dispose();

    public void Setup()
    {
        Context.GraphicsContext.SetPipelineState(Context.PipelineStateObjects["SimpleLit"], PipelineStateObjectDescription);
        Context.GraphicsContext.SetRootSignature(RootSignature);

        foreach (var texture in MaterialTextures)
            Context.GraphicsContext.SetShaderResourceView(Context.GetTextureByString(texture.Name), texture.Slot);
    }
}

public sealed partial class Material : EditorComponent, IHide
{
    public void SetMaterialTexture(params MaterialTextureEntry[] textureEntries)
    {
        MaterialTextures.AddRange(textureEntries);

        StringBuilder stringBuilder = new();
        for (int i = 0; i < textureEntries.Length; i++)
            stringBuilder.Append("s");

        var shaderResourceViews = stringBuilder.ToString();

        SetRootSignature("CC" + shaderResourceViews);
    }

    public void SetRootSignature(string rootSignatureParameters)
    {
        RootSignature?.Dispose();
        RootSignature = Context.CreateRootSignatureFromString(rootSignatureParameters);
    }
}