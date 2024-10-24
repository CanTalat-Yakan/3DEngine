using System.Collections.Generic;
using System.IO;

using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.DXGI;
using Vortice.Mathematics;

namespace Engine.Graphics;

public sealed partial class GraphicsContext : IDisposable
{
    public GraphicsDevice GraphicsDevice;

    public ID3D12GraphicsCommandList5 CommandList;
    public RootSignature CurrentRootSignature;

    public PipelineStateObject PipelineStateObject;
    public PipelineStateObjectDescription PipelineStateObjectDescription;

    public InputLayoutDescription InputLayoutDescription;

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;

        GraphicsDevice.Device.CreateCommandList(0, CommandListType.Direct, GraphicsDevice.GetGraphicsCommandAllocator(), null, out CommandList).ThrowIfFailed();
        CommandList.Close();
    }

    public void Dispose()
    {
        CommandList?.Dispose();
        CommandList = null;

        GC.SuppressFinalize(this);
    }
}

public sealed partial class GraphicsContext : IDisposable
{
    public void DrawIndexedInstanced(uint indexCountPerInstance, uint instanceCount, uint startIndexLocation, uint baseVertexLocation, uint startInstanceLocation)
    {
        CommandList.SetPipelineState(PipelineStateObject.GetState(GraphicsDevice, PipelineStateObjectDescription, CurrentRootSignature, InputLayoutDescription));
        CommandList.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, (int)baseVertexLocation, startInstanceLocation);
    }

    public void SetMesh(MeshData mesh, PrimitiveTopology topology = PrimitiveTopology.TriangleList)
    {
        CommandList.IASetPrimitiveTopology(topology);

        mesh.SetVertexBuffers();

        int previousInputSlot = -1;
        foreach (var inputElementDescription in mesh.InputLayoutDescription.Elements)
            if (inputElementDescription.Slot != previousInputSlot)
            {
                if (mesh.Vertices?.TryGetValue(inputElementDescription.SemanticName, out var vertex) ?? false)
                    CommandList.IASetVertexBuffers(inputElementDescription.Slot, new VertexBufferView(vertex.Resource.GPUVirtualAddress + vertex.Offset, vertex.SizeInByte - vertex.Offset, vertex.Stride));

                previousInputSlot = (int)inputElementDescription.Slot;
            }

        if (mesh.IndexBufferResource is not null)
            CommandList.IASetIndexBuffer(new IndexBufferView(mesh.IndexBufferResource.GPUVirtualAddress, mesh.IndexSizeInByte, mesh.IndexFormat));

        InputLayoutDescription = mesh.InputLayoutDescription;

        mesh.LastTimeUsed = DateTime.Now;
    }

    public void UploadMesh(MeshData mesh, float[] vertexData, int[] indexData, Format indexFormat)
    {
        var vertexFormat = Format.R32_Float;
        var vertexSizeInByte = GraphicsDevice.GetSizeInByte(vertexFormat);
        byte[] vertexByteSpan = new byte[vertexData.Length * vertexSizeInByte];
        System.Buffer.BlockCopy(vertexData, 0, vertexByteSpan, 0, vertexByteSpan.Length);

        var indexSizeInByte = GraphicsDevice.GetSizeInByte(indexFormat);
        byte[] indexByteSpan = new byte[indexData.Length * indexSizeInByte];
        System.Buffer.BlockCopy(indexData, 0, indexByteSpan, 0, indexByteSpan.Length);

        Kernel.Instance.Context.UploadBuffer.UploadVertexBuffer(mesh, vertexByteSpan);
        Kernel.Instance.Context.UploadBuffer.UploadIndexBuffer(mesh, indexByteSpan, indexFormat);
    }

    public void UploadTexture(Texture2D texture, List<byte[]> mipData)
    {
        uint mipLevels = texture.MipLevels;

        // Calculate the total size and get copyable footprints
        var resourceDescription = ResourceDescription.Texture2D(texture.Format, texture.Width, texture.Height, arraySize: 1, mipLevels: (ushort)mipLevels);

        ulong totalSize = 0;
        ulong[] rowSizesInBytes = new ulong[mipLevels];
        uint[] numRows = new uint[mipLevels];
        var layouts = new PlacedSubresourceFootPrint[mipLevels];

        GraphicsDevice.Device.GetCopyableFootprints(resourceDescription, 0, mipLevels, 0, layouts, numRows, rowSizesInBytes, out totalSize);

        Kernel.Instance.Context.UploadBuffer.UploadTexture(texture, mipData, layouts, numRows, rowSizesInBytes);
    }

    public ReadOnlyMemory<byte> LoadShader(DxcShaderStage shaderStage, string filePath, string entryPoint, bool fromResources = false)
    {
        filePath = fromResources ? AssetPaths.SHADERS + filePath : filePath;

        string directory = Path.GetDirectoryName(filePath);
        string shaderSource = File.ReadAllText(filePath);

        using (ShaderIncludeHandler includeHandler = new(directory))
        {
            using IDxcResult results = DxcCompiler.Compile(shaderStage, shaderSource, entryPoint, includeHandler: includeHandler);
            if (results.GetStatus().Failure)
                throw new Exception(results.GetErrors());

            return results.GetObjectBytecodeMemory();
        }
    }
}

public sealed partial class GraphicsContext : IDisposable
{
    public void BeginRender()
    {
        if (Kernel.Instance.Config.MultiSample == MultiSample.None)
            CommandList.ResourceBarrierTransition(GraphicsDevice.GetBackBufferRenderTarget(), ResourceStates.Present, ResourceStates.RenderTarget);
        else
            CommandList.ResourceBarrierTransition(GraphicsDevice.GetMSAARenderTarget(), ResourceStates.Common, ResourceStates.RenderTarget);
    }

    public void EndRender()
    {
        if (Kernel.Instance.Config.MultiSample == MultiSample.None)
            CommandList.ResourceBarrierTransition(GraphicsDevice.GetBackBufferRenderTarget(), ResourceStates.RenderTarget, ResourceStates.Present);
        else
        {
            // Transition MSAA render target to ResolveSource and transition back buffer to ResolveDestination
            CommandList.ResourceBarrierTransition(GraphicsDevice.GetMSAARenderTarget(), ResourceStates.RenderTarget, ResourceStates.ResolveSource);
            CommandList.ResourceBarrierTransition(GraphicsDevice.GetBackBufferRenderTarget(), ResourceStates.Present, ResourceStates.ResolveDest);

            // Resolve MSAA render target to back buffer
            CommandList.ResolveSubresource(GraphicsDevice.GetBackBufferRenderTarget(), 0, GraphicsDevice.GetMSAARenderTarget(), 0, GraphicsDevice.SwapChainFormat);

            // Transition MSAA render target back to Common and transition back buffer to Present
            CommandList.ResourceBarrierTransition(GraphicsDevice.GetMSAARenderTarget(), ResourceStates.ResolveSource, ResourceStates.Common);
            CommandList.ResourceBarrierTransition(GraphicsDevice.GetBackBufferRenderTarget(), ResourceStates.ResolveDest, ResourceStates.Present);
        }
    }

    public void BeginCommand() =>
        CommandList.Reset(GraphicsDevice.GetGraphicsCommandAllocator());

    public void EndCommand() =>
        CommandList.Close();

    public void Execute() =>
        GraphicsDevice.CommandQueue.ExecuteCommandList(CommandList);

    public void SetDescriptorHeapDefault() =>
        CommandList.SetDescriptorHeaps(1, new[] { GraphicsDevice.ShaderResourcesHeap.Heap });

    public void SetPipelineState(PipelineStateObject pipelineStateObject, PipelineStateObjectDescription pipelineStateObjectDescription)
    {
        PipelineStateObject = pipelineStateObject;
        PipelineStateObjectDescription = pipelineStateObjectDescription;
    }
    
    public void SetRootSignature(RootSignature rootSignature)
    {
        CurrentRootSignature = rootSignature;
        CommandList.SetGraphicsRootSignature(rootSignature.Resource);
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
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
            Format = texture.Format,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
        };
        shaderResourceViewDescription.Texture2D.MipLevels = texture.MipLevels;

        texture.StateChange(CommandList, ResourceStates.GenericRead);

        GraphicsDevice.ShaderResourcesHeap.GetTemporaryHandle(out var CPUHandle, out var GPUHandle);

        GraphicsDevice.Device.CreateShaderResourceView(texture.Resource, shaderResourceViewDescription, CPUHandle);

        CommandList.SetGraphicsRootDescriptorTable(CurrentRootSignature.ShaderResourceView[slot], GPUHandle);
    }

    public void SetRenderTarget()
    {
        CommandList.RSSetViewport(new Viewport(0, 0, GraphicsDevice.Size.Width, GraphicsDevice.Size.Height, 0.0f, 1.0f));
        CommandList.RSSetScissorRect(new RectI(0, 0, GraphicsDevice.Size.Width, GraphicsDevice.Size.Height));

        if (Kernel.Instance.Config.MultiSample == MultiSample.None)
            CommandList.OMSetRenderTargets(GraphicsDevice.GetRenderTargetHandle(), GraphicsDevice.GetDepthStencilHandle());
        else
            CommandList.OMSetRenderTargets(GraphicsDevice.GetMSAARenderTargetHandle(), GraphicsDevice.GetDepthStencilHandle());
    }

    public void ClearTexture2D(Texture2D texture2D) =>
        CommandList.ClearRenderTargetView(texture2D.RenderTargetView.GetCPUDescriptorHandleForHeapStart(), new Color4(0, 0, 0, 0));

    public void ClearRenderTarget(Color4? color = null)
    {
        color ??= new Color4(0.15f, 0.15f, 0.15f, 1);

        if (Kernel.Instance.Config.MultiSample == MultiSample.None)
            CommandList.ClearRenderTargetView(GraphicsDevice.GetRenderTargetHandle(), color.Value);
        else
            CommandList.ClearRenderTargetView(GraphicsDevice.GetMSAARenderTargetHandle(), color.Value);
    }

    public void ClearDepthStencil() =>
        CommandList.ClearDepthStencilView(GraphicsDevice.GetDepthStencilHandle(), ClearFlags.Depth | ClearFlags.Stencil, 1.0f, 0);
}