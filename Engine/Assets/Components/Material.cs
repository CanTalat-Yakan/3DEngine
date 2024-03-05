using System.Collections.Generic;

using Vortice.Direct3D12;

namespace Engine.Components;

public struct MaterialTextureEntry(string name, int slot)
{
    public string Name = name;
    public int Slot = slot;
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
        if (string.IsNullOrEmpty(PipelineStateObjectName))
            throw new NotImplementedException("error pipeline state object not set in material");

        PipelineStateObjectDescription.InputLayout = PipelineStateObjectName;

        Context.GraphicsContext.SetPipelineState(Context.PipelineStateObjects[PipelineStateObjectName], PipelineStateObjectDescription);
        Context.GraphicsContext.SetRootSignature(RootSignature);

        foreach (var texture in MaterialTextures)
            Context.GraphicsContext.SetShaderResourceView(Context.GetTextureByString(texture.Name), texture.Slot);

        if (Context.SerializableConstantBuffers.ContainsKey(PipelineStateObjectName))
        {
            Context.UploadBuffer.Upload(Context.SerializableConstantBuffers[PipelineStateObjectName], out var offset);
            Context.UploadBuffer.SetConstantBufferView(offset, 10);
        }
    }

    public bool Equals(Material other) =>
        PipelineStateObjectName == other.PipelineStateObjectName
     && MaterialTextures.Count == other.MaterialTextures.Count
     && RootSignature == other.RootSignature;

    public void SetPipelineStateObject(string pipelineStateObject)
    {
        if (Context.PipelineStateObjects.ContainsKey(pipelineStateObject))
            PipelineStateObjectName = pipelineStateObject;
        else throw new NotImplementedException("error pipeline state object not found in material");
    }

    public void SetRootSignature(string rootSignatureParameters)
    {
        RootSignature?.Dispose();
        RootSignature = Context.CreateRootSignatureFromString(rootSignatureParameters);
    }
}