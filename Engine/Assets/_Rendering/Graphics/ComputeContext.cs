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
        GraphicsDevice.GetComputeCommandAllocator().Reset();
        CommandList.Reset(GraphicsDevice.GetComputeCommandAllocator());
    }

    public void EndCommand() =>
        CommandList.Close();

    public void Execute() =>
        GraphicsDevice.CommandQueue.ExecuteCommandList(CommandList);

    public void SetDescriptorHeapDefault() =>
        CommandList.SetDescriptorHeaps(1, [GraphicsDevice.ShaderResourcesHeap.Heap]);

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
    
    public void SetConstantBufferView(uint offset, uint slot) =>
        CommandList.SetGraphicsRootConstantBufferView(CurrentRootSignature.ConstantBufferView[slot], Kernel.Instance.Context.UploadBuffer.Resource.GPUVirtualAddress + offset);
    
    public void SetUnorderedAccessView(uint offset, uint slot) =>
        CommandList.SetGraphicsRootUnorderedAccessView(CurrentRootSignature.ConstantBufferView[slot], Kernel.Instance.Context.UploadBuffer.Resource.GPUVirtualAddress + offset);

    public void SetShaderResourceView(Texture2D texture, uint slot)
    {
        const uint D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING = 5768;
        ShaderResourceViewDescription shaderResourceViewDescription = new()
        {
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Format = texture.Format,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
        };
        shaderResourceViewDescription.Texture2D.MipLevels = texture.MipLevels;

        texture.StateChange(CommandList, ResourceStates.GenericRead);

        GraphicsDevice.ShaderResourcesHeap.GetTemporaryHandle(out var CPUHandle, out var GPUHandle);

        GraphicsDevice.Device.CreateShaderResourceView(texture.Resource, shaderResourceViewDescription, CPUHandle);

        CommandList.SetGraphicsRootDescriptorTable(CurrentRootSignature.ShaderResourceView[slot], GPUHandle);
    }
}