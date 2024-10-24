using System.Collections.Generic;

using Vortice.Direct3D12;

namespace Engine.DataStructures;

public sealed class ComputePipelineStateObject : IDisposable
{
    public List<ComputePipelineStateObjectBundle> ComputePipelineStateObjectBundles = new();

    public ReadOnlyMemory<byte> ComputeShader;

    public string Name;

    public ComputePipelineStateObject(ReadOnlyMemory<byte> computeShader) =>
        ComputeShader = computeShader;

    public ID3D12PipelineState GetState(GraphicsDevice device, RootSignature rootSignature)
    {
        foreach (var bundle in ComputePipelineStateObjectBundles)
            if (bundle.RootSignature.Equals(rootSignature))
            {
                if (bundle.pipelineState is null)
                    throw new Exception("pipeline state error");

                return bundle.pipelineState;
            }

        ComputePipelineStateDescription computePipelineStateObjectDescription = new()
        {
            RootSignature = rootSignature.Resource,
            ComputeShader = ComputeShader
        };

        var pipelineState = device.Device.CreateComputePipelineState<ID3D12PipelineState>(computePipelineStateObjectDescription);
        if (pipelineState is null)
            throw new Exception("pipeline state error");

        ComputePipelineStateObjectBundles.Add(new()
        {
            pipelineState = pipelineState,
            RootSignature = rootSignature
        });

        return pipelineState;
    }

    public void Dispose()
    {
        foreach (var bundle in ComputePipelineStateObjectBundles)
            bundle.pipelineState.Dispose();

        ComputePipelineStateObjectBundles.Clear();

        GC.SuppressFinalize(this);
    }
}

public sealed class ComputePipelineStateObjectBundle
{
    public ComputePipelineStateObjectDescription PipelineStateObjectDescription;
    public RootSignature RootSignature;
    public ID3D12PipelineState pipelineState;
}

public struct ComputePipelineStateObjectDescription : IEquatable<ComputePipelineStateObjectDescription>
{
    public string RootSignatureName;

    public override bool Equals(object obj) =>
        obj is ComputePipelineStateObjectDescription description && Equals(description);

    public bool Equals(ComputePipelineStateObjectDescription other) =>
        RootSignatureName == other.RootSignatureName;

    public override int GetHashCode()
    {
        return RootSignatureName.GetHashCode();
    }

    public static bool operator ==(ComputePipelineStateObjectDescription x, ComputePipelineStateObjectDescription y) =>
        x.Equals(y);

    public static bool operator !=(ComputePipelineStateObjectDescription x, ComputePipelineStateObjectDescription y) =>
        !(x == y);
}