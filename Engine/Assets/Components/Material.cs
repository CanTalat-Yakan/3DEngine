using System.Collections.Generic;

using Vortice.Direct3D12;

namespace Engine.Components;

public struct MaterialTextureEntry(string name, uint slot)
{
    public string Name = name;
    public uint Slot = slot;
}

public sealed partial class Material : EditorComponent, IHide, IEquatable<Material>
{
    public string PipelineStateObjectName { get; private set; }
    public RootSignature RootSignature { get; private set; }

    public List<MaterialTextureEntry> MaterialTextures { get; private set; } = new();

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public PipelineStateObjectDescription PipelineStateObjectDescription = new()
    {
        CullMode = CullMode.Front,
        Wireframe = true,
        BlendState = "Alpha",
    };

    public override void OnDestroy() =>
        RootSignature?.Dispose();

    public void Setup()
    {
        if (!Context.IsRendering)
            return;

        if (string.IsNullOrEmpty(PipelineStateObjectName))
            throw new NotImplementedException("error pipeline state object not set in material");

        PipelineStateObjectDescription.Wireframe = Camera.Main?.RenderMode switch
        {
            RenderMode.Shaded => false,
            RenderMode.Wireframe => true,
            _ => false
        };

        PipelineStateObjectDescription.InputLayout = PipelineStateObjectName;

        Context.GraphicsContext.SetPipelineState(Assets.PipelineStateObjects[PipelineStateObjectName], PipelineStateObjectDescription);
        Context.GraphicsContext.SetRootSignature(RootSignature);

        foreach (var texture in MaterialTextures)
            Context.GraphicsContext.SetShaderResourceView(Context.GetTextureByString(texture.Name), texture.Slot);

        if (Assets.SerializableConstantBuffers.ContainsKey(PipelineStateObjectName))
        {
            Context.UploadBuffer.Upload(Assets.SerializableConstantBuffers[PipelineStateObjectName], out var offset);
            Context.GraphicsContext.SetConstantBufferView(offset, 10);
        }
    }
}

public sealed partial class Material : EditorComponent, IHide, IEquatable<Material>
{
    public bool Equals(Material other) =>
        PipelineStateObjectName == other.PipelineStateObjectName
     && MaterialTextures.Count == other.MaterialTextures.Count
     && RootSignature == other.RootSignature;

    public void SetRootSignature(RootSignatureHelper rootSignatureHelper = null) =>
        SetRootSignature(rootSignatureHelper?.GetString() ?? RootSignatureHelper.GetDefault());

    public void SetRootSignature(string rootSignatureParameters)
    {
        if (!Context.IsRendering)
            return;

        RootSignature?.Dispose();
        RootSignature = Context.CreateRootSignatureFromString(rootSignatureParameters);
    }

    public void SetPipeline<T>(T shaderFile) where T : Enum =>
        SetPipelineStateObject(shaderFile.ToString());

    public void SetPipeline(ShaderFiles shaderFile) =>
        SetPipelineStateObject(shaderFile.ToString());

    public void SetPipeline(string pipelineStateObjectName) =>
        SetPipelineStateObject(pipelineStateObjectName);

    public void SetPipelineStateObject(string pipelineStateObject)
    {
        if (!Context.IsRendering)
            return;

        if (Assets.PipelineStateObjects.ContainsKey(pipelineStateObject))
            PipelineStateObjectName = pipelineStateObject;
        else throw new NotImplementedException("error pipeline state object not found in material");
    }

    public void SetTextures<T>(params T[] textureEntries) where T : Enum
    {
        var materialTextureEntries = new MaterialTextureEntry[textureEntries.Length];
        for (uint i = 0; i < textureEntries.Length; i++)
            materialTextureEntries[i] = new(textureEntries[i].ToString() + ".png", i);

        MaterialTextures.AddRange(materialTextureEntries);
    }

    public void SetTextures(params TextureFiles[] textureFiles)
    {
        var materialTextureEntries = new MaterialTextureEntry[textureFiles.Length];
        for (uint i = 0; i < textureFiles.Length; i++)
            materialTextureEntries[i] = new(textureFiles[i] + ".png", i);

        SetTextures(materialTextureEntries);
    }

    public void SetTextures(params MaterialTextureEntry[] textureEntries) =>
        MaterialTextures.AddRange(textureEntries);
}