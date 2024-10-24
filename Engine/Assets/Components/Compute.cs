using System.Collections.Generic;

namespace Engine.Components;

public struct ComputeTextureEntry(string name, uint slot)
{
    public string Name = name;
    public uint Slot = slot;
}

public sealed partial class Compute : EditorComponent, IHide, IEquatable<Compute>
{
    public string ComputePipelineStateObjectName { get; private set; }
    public RootSignature ComputeRootSignature { get; private set; }

    public List<ComputeTextureEntry> ComputeTextures { get; private set; } = new();

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public ComputePipelineStateObjectDescription ComputePipelineStateObjectDescription = new();

    public override void OnDestroy() =>
        ComputeRootSignature?.Dispose();

    public void Setup<T>(T data)
    {
        if (!Context.IsRendering)
            return;

        if (string.IsNullOrEmpty(ComputePipelineStateObjectName))
            throw new NotImplementedException("error pipeline state object not set in material");

        Context.GraphicsContext.SetComputePipelineState(Assets.ComputePipelineStateObjects[ComputePipelineStateObjectName], ComputePipelineStateObjectDescription);
        Context.GraphicsContext.SetComputeRootSignature(ComputeRootSignature);

        foreach (var computeTexture in ComputeTextures)
            Context.GraphicsContext.SetShaderResourceView(Context.GetTextureByString(computeTexture.Name), computeTexture.Slot);

        if (data is not null)
        {
            Context.UploadBuffer.Upload(data, out var offset);
            Context.UploadBuffer.SetUnorderedAccessView(offset, 0);
        }
    }

    public void Dispatch(uint threadGroupsX, uint threadGroupsY, uint threadGroupsZ) =>
        Context.GraphicsContext.ComputeCommandList.Dispatch(threadGroupsX, threadGroupsY, threadGroupsZ);

    public bool Equals(Compute other) =>
        ComputePipelineStateObjectName == other.ComputePipelineStateObjectName
     && ComputeTextures.Count == other.ComputeTextures.Count
     && ComputeRootSignature == other.ComputeRootSignature;

    public void SetComputePipelineStateObject(string computePipelineStateObject)
    {
        if (!Context.IsRendering)
            return;

        if (Assets.ComputePipelineStateObjects.ContainsKey(computePipelineStateObject))
            ComputePipelineStateObjectName = computePipelineStateObject;
        else throw new NotImplementedException("error compute pipeline state object not found in material");
    }

    public void SetRootSignature(string computeRootSignatureParameters)
    {
        if (!Context.IsRendering)
            return;

        ComputeRootSignature?.Dispose();
        ComputeRootSignature = Context.CreateRootSignatureFromString(computeRootSignatureParameters);
    }
}