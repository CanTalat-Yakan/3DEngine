using Vortice.Direct3D12;

namespace Engine.Graphics;

public sealed partial class ComputeContext : IDisposable
{
    public GraphicsDevice GraphicsDevice;

    public ID3D12GraphicsCommandList5 CommandList;
    public RootSignature CurrentRootSignature;

    public ComputePipelineStateObject ComputePipelineStateObject;
    public ComputePipelineStateObjectDescription ComputePipelineStateObjectDescription;

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;

        GraphicsDevice.Device.CreateCommandList(0, CommandListType.Compute, GraphicsDevice.GetComputeCommandAllocator(), null, out CommandList).ThrowIfFailed();
        CommandList.Close();
    }

    public void Dispose()
    {
        CommandList?.Dispose();
        CommandList = null;

        GC.SuppressFinalize(this);
    }
}

public sealed partial class ComputeContext : IDisposable
{
    public void BeginCommand()
    {
        CommandList.SetDescriptorHeaps(1, [GraphicsDevice.ShaderResourcesHeap.Heap]);

        GraphicsDevice.GetComputeCommandAllocator().Reset();
        CommandList.Reset(GraphicsDevice.GetComputeCommandAllocator());
    }

    public void EndCommand() =>
        CommandList.Close();

    public void Execute() =>
        GraphicsDevice.CommandQueue.ExecuteCommandList(CommandList);

    public void SetPipelineState(ComputePipelineStateObject computePipelineStateObject, ComputePipelineStateObjectDescription computePipelineStateObjectDescription)
    {
        ComputePipelineStateObject = computePipelineStateObject;
        ComputePipelineStateObjectDescription = computePipelineStateObjectDescription;
    }

    public void SetRootSignature(RootSignature rootSignature)
    {
        CurrentRootSignature = rootSignature;
        CommandList.SetComputeRootSignature(rootSignature.Resource);
    }

    public void SetConstantBufferView(UploadBuffer uploadBuffer, uint offset, uint slot) =>
        CommandList.SetGraphicsRootConstantBufferView(CurrentRootSignature.ConstantBufferView[slot], uploadBuffer.Resource.GPUVirtualAddress + offset);

    public void SetUnorderedAccessView(UploadBuffer uploadBuffer, uint offset, uint slot) =>
        CommandList.SetGraphicsRootUnorderedAccessView(CurrentRootSignature.UnorderedAccessView[slot], uploadBuffer.Resource.GPUVirtualAddress + offset);

    public void SetShaderResourceView(Texture2D texture2D, uint slot)
    {
        texture2D.StateChange(CommandList, ResourceStates.UnorderedAccess);

        CommandList.SetGraphicsRootDescriptorTable(CurrentRootSignature.ShaderResourceView[slot], GraphicsDevice.GetShaderResourceHandleGPU(texture2D));
    }
}