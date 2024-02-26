using System.Runtime.InteropServices;

using Vortice.Direct3D;
using Vortice.Direct3D12;
using Vortice.Mathematics;

using Vortice.Dxc;
using System.IO;

namespace Engine.Graphics;

public sealed partial class GraphicsContext : IDisposable
{
    public ID3D12GraphicsCommandList5 CommandList;
    public GraphicsDevice GraphicsDevice;

    public RootSignature CurrentRootSignature;

    public PipelineStateObject PipelineStateObject;
    public PipelineStateObjectDescription PipelineStateObjectDescription;

    public InputLayoutDescription InputLayoutDescription;

    public void Initialize(GraphicsDevice graphicsDevice)
    {
        GraphicsDevice = graphicsDevice;

        GraphicsDevice.Device.CreateCommandList(0, CommandListType.Direct, graphicsDevice.GetCommandAllocator(), null, out CommandList).ThrowIfFailed();
        CommandList.Close();
    }

    public void Dispose()
    {
        CommandList?.Dispose();
        CommandList = null;
    }
}

public sealed partial class GraphicsContext : IDisposable
{
    public void DrawIndexedInstanced(int indexCountPerInstance, int instanceCount, int startIndexLocation, int baseVertexLocation, int startInstanceLocation)
    {
        CommandList.SetPipelineState(PipelineStateObject.GetState(GraphicsDevice, PipelineStateObjectDescription, CurrentRootSignature, InputLayoutDescription));
        CommandList.DrawIndexedInstanced(indexCountPerInstance, instanceCount, startIndexLocation, baseVertexLocation, startInstanceLocation);
    }

    public void ScreenBeginRender() =>
        CommandList.ResourceBarrierTransition(GraphicsDevice.GetScreenResource(), ResourceStates.Present, ResourceStates.RenderTarget);

    public void ScreenEndRender() =>
        CommandList.ResourceBarrierTransition(GraphicsDevice.GetScreenResource(), ResourceStates.RenderTarget, ResourceStates.Present);

    public void BeginCommand() =>
        CommandList.Reset(GraphicsDevice.GetCommandAllocator());

    public void EndCommand() =>
        CommandList.Close();

    public void Execute() =>
        GraphicsDevice.CommandQueue.ExecuteCommandList(CommandList);
}

public sealed partial class GraphicsContext : IDisposable
{
    public ReadOnlyMemory<byte> LoadShader(DxcShaderStage shaderStage, string filePath, string entryPoint)
    {
        string directoryPath = AppContext.BaseDirectory + @"Assets\Resources\Shaders\";

        string directory = Path.GetDirectoryName(filePath);
        string shaderSource = File.ReadAllText(filePath);

        using (ShaderIncludeHandler includeHandler = new(directoryPath, directory))
        {
            using IDxcResult results = DxcCompiler.Compile(shaderStage, shaderSource, entryPoint, includeHandler: includeHandler);
            if (results.GetStatus().Failure)
                throw new Exception(results.GetErrors());

            return results.GetObjectBytecodeMemory();
        }
    }

    public void UploadTexture(Texture2D texture, byte[] data)
    {
        ID3D12Resource resourceUpload = GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
            new HeapProperties(HeapType.Upload),
            HeapFlags.None,
            ResourceDescription.Buffer((ulong)data.Length),
            ResourceStates.GenericRead);
        GraphicsDevice.DestroyResource(resourceUpload);

        GraphicsDevice.DestroyResource(texture.Resource);
        texture.Resource = GraphicsDevice.Device.CreateCommittedResource<ID3D12Resource>(
            HeapProperties.DefaultHeapProperties,
            HeapFlags.None,
            ResourceDescription.Texture2D(texture.Format, (uint)texture.Width, (uint)texture.Height, arraySize: 1, mipLevels: 1),
            ResourceStates.CopyDest);

        uint bitsPerPixel = GraphicsDevice.GetBitsPerPixel(texture.Format);

        SubresourceData subresourcedata = new()
        {
            Data = Marshal.UnsafeAddrOfPinnedArrayElement(data, 0),
            RowPitch = (IntPtr)(texture.Width * bitsPerPixel / 8),
            SlicePitch = (IntPtr)(texture.Width * texture.Height * bitsPerPixel / 8),
        };
        UpdateSubresources(CommandList, texture.Resource, resourceUpload, 0, 0, 1, [subresourcedata]);

        GCHandle gcHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
        gcHandle.Free();

        CommandList.ResourceBarrierTransition(texture.Resource, ResourceStates.CopyDest, ResourceStates.GenericRead);
        texture.ResourceStates = ResourceStates.GenericRead;
    }

    public void SetMesh(MeshInfo mesh)
    {
        CommandList.IASetPrimitiveTopology(PrimitiveTopology.TriangleList);

        int previousInputSlot = -1;
        foreach (var inputElementDescription in mesh.InputLayoutDescription.Elements)
            if (inputElementDescription.Slot != previousInputSlot)
            {
                if (mesh.Vertices?.TryGetValue(inputElementDescription.SemanticName, out var vertex) ?? false)
                    CommandList.IASetVertexBuffers(inputElementDescription.Slot, new VertexBufferView(vertex.Resource.GPUVirtualAddress + (ulong)vertex.Offset, vertex.SizeInByte - vertex.Offset, vertex.Stride));

                previousInputSlot = inputElementDescription.Slot;
            }

        if (mesh.IndexBufferResource is not null)
            CommandList.IASetIndexBuffer(new IndexBufferView(mesh.IndexBufferResource.GPUVirtualAddress, mesh.IndexSizeInByte, mesh.IndexFormat));

        InputLayoutDescription = mesh.InputLayoutDescription;
    }
}

public sealed partial class GraphicsContext : IDisposable
{
    public void SetDescriptorHeapDefault()
    {
        CommandList.SetDescriptorHeaps(1, new[] { GraphicsDevice.ShaderResourcesHeap.Heap });
    }

    public void SetRootSignature(RootSignature rootSignature)
    {
        CurrentRootSignature = rootSignature;
        CommandList.SetGraphicsRootSignature(rootSignature.Resource);
    }

    public void SetComputeRootSignature(RootSignature rootSignature)
    {
        CurrentRootSignature = rootSignature;
        CommandList.SetComputeRootSignature(rootSignature.Resource);
    }

    public void SetShaderResourceView(Texture2D texture, int slot)
    {
        const int D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING = 5768;
        ShaderResourceViewDescription shaderResourceViewDescription = new()
        {
            ViewDimension = Vortice.Direct3D12.ShaderResourceViewDimension.Texture2D,
            Format = texture.Format,
            Shader4ComponentMapping = D3D12_DEFAULT_SHADER_4_COMPONENT_MAPPING,
        };
        shaderResourceViewDescription.Texture2D.MipLevels = texture.MipLevels;

        texture.StateChange(CommandList, ResourceStates.GenericRead);

        GraphicsDevice.ShaderResourcesHeap.GetTemporaryHandle(out CpuDescriptorHandle CPUHandle, out GpuDescriptorHandle GPUHandle);
        GraphicsDevice.Device.CreateShaderResourceView(texture.Resource, shaderResourceViewDescription, CPUHandle);

        CommandList.SetGraphicsRootDescriptorTable(CurrentRootSignature.ShaderResourceView[slot], GPUHandle);
    }

    public void SetDepthStencilViewRenderTextureView(Texture2D depthStencilView, Texture2D[] renderTextureViews, bool clearDepthStencilView, bool clearRenderTextureView)
    {
        depthStencilView?.StateChange(CommandList, ResourceStates.DepthWrite);

        CpuDescriptorHandle[] renderTextureViewHandles = null;
        if (renderTextureViews is not null)
        {
            renderTextureViewHandles = new CpuDescriptorHandle[renderTextureViews.Length];
            for (int i = 0; i < renderTextureViews.Length; i++)
            {
                Texture2D renderTextureView = renderTextureViews[i];
                renderTextureView.StateChange(CommandList, ResourceStates.RenderTarget);
                renderTextureViewHandles[i] = renderTextureView.RenderTargetView.GetCPUDescriptorHandleForHeapStart();
            }
        }

        if (clearDepthStencilView && depthStencilView is not null)
            CommandList.ClearDepthStencilView(depthStencilView.DepthStencilView.GetCPUDescriptorHandleForHeapStart(), ClearFlags.Depth | ClearFlags.Stencil, 1.0f, 0);

        if (clearRenderTextureView && renderTextureViews is not null)
            foreach (var rtv in renderTextureViews)
                CommandList.ClearRenderTargetView(rtv.RenderTargetView.GetCPUDescriptorHandleForHeapStart(), new Color4());

        CommandList.OMSetRenderTargets(renderTextureViewHandles, depthStencilView.DepthStencilView.GetCPUDescriptorHandleForHeapStart());
    }

    public void SetRenderTargetScreen()
    {
        CommandList.RSSetViewport(new Viewport(0, 0, GraphicsDevice.Size.Width, GraphicsDevice.Size.Height, 0.0f, 1.0f));
        CommandList.RSSetScissorRect(new RectI(0, 0, GraphicsDevice.Size.Width, GraphicsDevice.Size.Height));
        CommandList.OMSetRenderTargets(GraphicsDevice.GetRenderTargetScreen());
    }

    public void SetConstantBufferView(UploadBuffer uploadBuffer, int offset, int slot)
    {
        CommandList.SetGraphicsRootConstantBufferView(CurrentRootSignature.ConstantBufferView[slot], uploadBuffer.Resource.GPUVirtualAddress + (ulong)offset);
    }

    public void SetPipelineState(PipelineStateObject pipelineStateObject, PipelineStateObjectDescription pipelineStateObjectDescription)
    {
        PipelineStateObject = pipelineStateObject;
        PipelineStateObjectDescription = pipelineStateObjectDescription;
    }

    public void ClearRenderTarget(Texture2D texture2D)
    {
        CommandList.ClearRenderTargetView(texture2D.RenderTargetView.GetCPUDescriptorHandleForHeapStart(), new Color4(0, 0, 0, 0));
    }

    public void ClearRenderTargetScreen(Color4? color = null)
    {
        color ??= new Color4(0.15f, 0.15f, 0.15f, 1);

        CommandList.ClearRenderTargetView(GraphicsDevice.GetRenderTargetScreen(), color.Value);
    }
}

public sealed partial class GraphicsContext : IDisposable
{
    private unsafe struct MemoryCopyDestination
    {
        public void* Data;
        public ulong RowPitch;
        public ulong SlicePitch;
    }

    private unsafe void MemoryCopySubresource(
        MemoryCopyDestination* destination,
        SubresourceData source,
        int rowSizeInBytes,
        int numberOfRows,
        int numberOfSlices)
    {
        for (uint i = 0; i < numberOfSlices; ++i)
        {
            byte* destinationSlice = (byte*)destination->Data + destination->SlicePitch * i;
            byte* sourceSlice = (byte*)source.Data + source.SlicePitch * i;

            for (int y = 0; y < numberOfRows; ++y)
            {
                Span<byte> sourceSpan = new(sourceSlice + ((long)source.RowPitch * y), rowSizeInBytes);
                Span<byte> destinationSpan = new(destinationSlice + (long)destination->RowPitch * y, rowSizeInBytes);

                sourceSpan.CopyTo(destinationSpan);
            }
        }
    }

    private unsafe ulong UpdateSubresources(
        ID3D12GraphicsCommandList commandList,
        ID3D12Resource destinationResource,
        ID3D12Resource intermediate,
        int firstSubresource,
        int numberOfSubresources,
        ulong requiredSize,
        PlacedSubresourceFootPrint[] layouts,
        int[] numberOfRows,
        ulong[] rowSizesInBytes,
        SubresourceData[] sourceData)
    {
        var intermediateDescription = intermediate.Description;
        var destinationDescription = destinationResource.Description;

        if (intermediateDescription.Dimension != ResourceDimension.Buffer
         || intermediateDescription.Width < requiredSize + layouts[0].Offset
         || (destinationDescription.Dimension == ResourceDimension.Buffer
         && (firstSubresource != 0 || numberOfSubresources != 1)))
            return 0;

        void* pointer = null;
        intermediate.Map(0, &pointer);
        byte* data = (byte*)new IntPtr(pointer);

        for (uint i = 0; i < numberOfSubresources; ++i)
        {
            MemoryCopyDestination destinationData = new()
            {
                Data = data + layouts[i].Offset,
                RowPitch = (ulong)layouts[i].Footprint.RowPitch,
                SlicePitch = (uint)(layouts[i].Footprint.RowPitch) * (uint)(numberOfRows[i])
            };
            MemoryCopySubresource(&destinationData, sourceData[i], (int)(rowSizesInBytes[i]), numberOfRows[i], layouts[i].Footprint.Depth);
        }
        intermediate.Unmap(0, null);

        if (destinationDescription.Dimension.Equals(ResourceDimension.Buffer))
        {
            commandList.CopyBufferRegion(destinationResource, 0, intermediate, layouts[0].Offset, (ulong)layouts[0].Footprint.Width);
        }
        else
        {
            for (int i = 0; i < numberOfSubresources; ++i)
            {
                TextureCopyLocation destination = new(destinationResource, i + firstSubresource);
                TextureCopyLocation source = new(intermediate, layouts[i]);
                commandList.CopyTextureRegion(destination, 0, 0, 0, source, null);
            }
        }

        return requiredSize;
    }

    private ulong UpdateSubresources(
        ID3D12GraphicsCommandList commandList,
        ID3D12Resource destinationResource,
        ID3D12Resource intermediate,
        ulong intermediateOffset,
        int firstSubresource,
        int numSubresources,
        SubresourceData[] sourceData)
    {
        PlacedSubresourceFootPrint[] layouts = new PlacedSubresourceFootPrint[numSubresources];
        ulong[] rowSizesInBytes = new ulong[numSubresources];
        int[] numRows = new int[numSubresources];

        destinationResource.GetDevice(out ID3D12Device device);
        device.GetCopyableFootprints(destinationResource.Description, firstSubresource, numSubresources, intermediateOffset, layouts, numRows, rowSizesInBytes, out ulong RequiredSize);
        device.Release();

        ulong Result = UpdateSubresources(commandList, destinationResource, intermediate, firstSubresource, numSubresources, RequiredSize, layouts, numRows, rowSizesInBytes, sourceData);
        return Result;
    }
}