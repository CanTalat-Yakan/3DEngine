using System.Collections.Generic;

using Vortice.Direct3D12;

namespace Engine.Components;

public struct ComputeTextureEntry(string name, uint slot)
{
    public string Name = name;
    public uint Slot = slot;
}

public sealed partial class Compute : EditorComponent, IHide, IEquatable<Compute>
{
    public string ComputePipelineStateObjectName { get; private set; }
    public RootSignature RootSignature { get; private set; }

    public List<ComputeTextureEntry> ComputeTextures { get; private set; } = new();

    public CommonContext Context => _context ??= Kernel.Instance.Context;
    public CommonContext _context;

    public ComputePipelineStateObjectDescription ComputePipelineStateObjectDescription = new();

    public override void OnDestroy() =>
        RootSignature?.Dispose();

    public void Setup<T>(T data)
    {
        if (!Context.IsRendering)
            return;

        if (string.IsNullOrEmpty(ComputePipelineStateObjectName))
            throw new NotImplementedException("error pipeline state object not set in material");

        Context.GraphicsContext.SetComputePipelineState(Assets.ComputePipelineStateObjects[ComputePipelineStateObjectName], ComputePipelineStateObjectDescription);
        Context.GraphicsContext.SetComputeRootSignature(RootSignature);

        foreach (var texture in ComputeTextures)
            Context.GraphicsContext.SetShaderResourceView(Context.GetTextureByString(texture.Name), texture.Slot);

        if (Assets.SerializableConstantBuffers.ContainsKey(ComputePipelineStateObjectName))
        {
            Context.UploadBuffer.Upload(data, out var offset);
            Context.UploadBuffer.SetConstantBufferView(offset, 0);
        }
    }

    public bool Equals(Compute other) =>
        ComputePipelineStateObjectName == other.ComputePipelineStateObjectName
     && ComputeTextures.Count == other.ComputeTextures.Count
     && RootSignature == other.RootSignature;

    public void SetComputePipelineStateObject(string computePipelineStateObject)
    {
        if (!Context.IsRendering)
            return;

        if (Assets.ComputePipelineStateObjects.ContainsKey(computePipelineStateObject))
            ComputePipelineStateObjectName = computePipelineStateObject;
        else throw new NotImplementedException("error compute pipeline state object not found in material");
    }

    public void SetRootSignature(string rootSignatureParameters)
    {
        if (!Context.IsRendering)
            return;

        RootSignature?.Dispose();
        RootSignature = Context.CreateRootSignatureFromString(rootSignatureParameters);
    }
}