using System.Collections.Generic;
using System.IO;

using SharpGen.Runtime;
using Vortice;
using Vortice.Direct3D12;
using Vortice.Dxc;
using Vortice.DXGI;
using Vortice.Mathematics;

using ImDrawIdx = System.UInt16;

namespace Engine.GUI;

unsafe public sealed partial class GUIRenderer
{
    public ID3D12RootSignature RootSignature;

    public ID3D12PipelineState PipelineState;
    public ID3D12GraphicsCommandList CommandList;
    public ID3D12CommandAllocator CommandAllocator;

    private Dictionary<IntPtr, ID3D12Resource> _textureResources = new();

    private ID3D12DescriptorHeap _textureView;
    private ID3D12DescriptorHeap _sampler;

    private ID3D12Resource _view;

    private ID3D12Resource _vertexBuffer;
    private ID3D12Resource _indexBuffer;

    private int _vertexBufferSize = 5000, _indexBufferSize = 10000;
    private const int _vertexConstantBufferSize = 16 * 4;

    public Renderer Renderer => _renderer ??= Renderer.Instance;
    private Renderer _renderer;

    public GUIRenderer()
    {
        ImGui.GetIO().BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;  // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.

        CreateRootSignature();

        CreateInputLayout(out var inputLayoutDescription);
        CreateShaderByteCode("ImGui.hlsl", out var vertexShaderByteCode, out var pixelShaderByteCode);
        CreateGraphicsPipelineState(inputLayoutDescription, vertexShaderByteCode, pixelShaderByteCode);

        CreateCommandAllocator();
        CreateCommandList();

        //CreateFontTextureAndSampler();
    }

    public void Update(IntPtr imGuiContext, SizeI newSize)
    {
        ImGui.SetCurrentContext(imGuiContext);
        var io = ImGui.GetIO();

        io.DeltaTime = Time.DeltaF;
        io.DisplaySize = newSize.ToVector2();

        ImGui.NewFrame();
    }

    public void Render() =>
        Draw(ImGui.GetDrawData());

    public void Dispose()
    {
        RootSignature?.Dispose();

        CommandAllocator?.Dispose();
        CommandList?.Dispose();
        PipelineState?.Dispose();

        _textureView?.Dispose();
        _sampler?.Dispose();
    }
}

unsafe public sealed partial class GUIRenderer
{
    private void Draw(ImDrawDataPtr data)
    {
        // Record rendering commands into a DirectX 12 command list.
        CommandAllocator.Reset();
        CommandList.Reset(CommandAllocator, PipelineState); // Use the pipeline state.

        // Avoid rendering when minimized.
        if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            return;

        // Create and initialize view constant buffer.
        CreateViewConstantBuffer(data);

        // Set root signature.
        CommandList.SetGraphicsRootSignature(RootSignature);

        // Set view constant buffer.
        CommandList.SetGraphicsRootConstantBufferView(0, _view.GPUVirtualAddress);

        // Create and update vertex and index buffer.
        CreateBuffers(data);
        UpdateBuffers(data);

        // Render ImGui draw data.
        RenderImDrawData(data);

        // Close and execute the command list.
        CommandList.Close();
        Renderer.Data.GraphicsQueue.ExecuteCommandList(CommandList);
    }

    private void RenderImDrawData(ImDrawDataPtr data)
    {
        //// Record ImGui draw commands.
        //for (int n = 0; n < data.CmdListsCount; n++)
        //{
        //    ImDrawListPtr cmdList = data.CmdListsRange[n];

        //    // Render command lists.
        //    for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
        //    {
        //        ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdIndex];

        //        // Set render target.
        //        CommandList.OMSetRenderTargets(Renderer.Data.BufferRenderTargetView.GetCPUDescriptorHandleForHeapStart(), null);

        //        // Render the command.
        //        CommandList.DrawIndexedInstanced((int)cmd.ElemCount, 1, (int)cmd.IdxOffset, (int)cmd.VtxOffset, 0);
        //    }
        //}

        // (Because we merged all buffers into a single one, we maintain our own offset into them)
        int global_idx_offset = 0;
        int global_vtx_offset = 0;
        Vector2 clip_off = data.DisplayPos;
        for (int n = 0; n < data.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = data.CmdListsRange[n];
            for (int i = 0; i < cmdList.CmdBuffer.Size; i++)
            {
                ImDrawCmdPtr cmd = cmdList.CmdBuffer[i];

                if (cmd.UserCallback != IntPtr.Zero)
                    throw new NotImplementedException("user callbacks not implemented");
                else
                {
                    cmd.ClipRect *= (float)Renderer.Instance.Config.ResolutionScale;

                    var rect = new RawRect((int)(cmd.ClipRect.X - clip_off.X), (int)(cmd.ClipRect.Y - clip_off.Y), (int)(cmd.ClipRect.Z - clip_off.X), (int)(cmd.ClipRect.W - clip_off.Y));
                    CommandList.RSSetScissorRects(new[] { rect });

                    _textureResources.TryGetValue(cmd.TextureId, out var texture);
                    //if (texture != null)
                        //CommandList.SetGraphicsRootShaderResourceView(0, new[] { texture });

                    CommandList.DrawIndexedInstanced((int)cmd.ElemCount, 0, (int)(cmd.IdxOffset + global_idx_offset), (int)(cmd.VtxOffset + global_vtx_offset), 0);
                }
            }
        }

        // Set vertex and index buffer.
        CommandList.IASetVertexBuffers(0, new VertexBufferView(_vertexBuffer.GPUVirtualAddress, _vertexBufferSize, sizeof(ImDrawVert)));
        CommandList.IASetIndexBuffer(new IndexBufferView(_indexBuffer.GPUVirtualAddress, _indexBufferSize, sizeof(ImDrawIdx) == 2));
    }

    private void CreateViewConstantBuffer(ImDrawDataPtr data)
    {
        //Create View Constant Buffer.
        _view = Renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(sizeof(ViewConstantBuffer)),
            ResourceStates.GenericRead);
        _view.Name = "ImGui View ConstantBuffer";

        float L = data.DisplayPos.X;
        float R = data.DisplayPos.X + data.DisplaySize.X;
        float T = data.DisplayPos.Y;
        float B = data.DisplayPos.Y + data.DisplaySize.Y;
        Matrix4x4 mvp = new(
            2.0f / (R - L), 0.0f, 0.0f, 0.0f,
            0.0f, 2.0f / (T - B), 0.0f, 0.0f,
            0.0f, 0.0f, 0.5f, 0.0f,
            (R + L) / (L - R), (T + B) / (B - T), 0.5f, 1.0f);

        // Map the constant buffer and copy the view-projection matrix and position of the camera into it.
        ViewConstantBuffer* pointer = _view.Map<ViewConstantBuffer>(0);
        // Copy the data from the new constant buffer to the mapped resource.
        *pointer = new ViewConstantBuffer(mvp, Vector3.Zero);
        // Unmap the constant buffer from memory.
        _view.Unmap(0);
    }

    private void CreateFontTextureAndSampler()
    {
        ImGui.GetIO().Fonts.GetTexDataAsRGBA32(
            out byte* pixels,
            out int width,
            out int height);


        #region // Create Texture
        ResourceDescription textureDescription = ResourceDescription.Texture2D(
            Format.R8G8B8A8_UNorm,
            (uint)width,
            (uint)height,
            arraySize: 1,
            mipLevels: 1);

        Result result = Renderer.Device.CreateCommittedResource(
            HeapProperties.DefaultHeapProperties,
            HeapFlags.None,
            textureDescription,
            ResourceStates.CopyDest,
            null,
            out var texture);

        if (result.Failure)
            throw new Exception(result.Description);

        texture.Name = "ImGui FontTexture";


        ShaderResourceViewDescription shaderResourceViewDescription = new()
        {
            Format = texture.Description.Format,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView { MipLevels = texture.Description.MipLevels },
            Shader4ComponentMapping = ShaderComponentMapping.Default
        };

        _textureView = Renderer.Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Flags = DescriptorHeapFlags.ShaderVisible,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
        });
        _textureView.Name = texture.Name + " Texture View";
        Renderer.Device.CreateShaderResourceView(texture, shaderResourceViewDescription, _textureView.GetCPUDescriptorHandleForHeapStart());

        var size = Renderer.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        _textureView.GetCPUDescriptorHandleForHeapStart().Offset(size);

        var id = texture.NativePointer;
        _textureResources.Add(id, texture);




        SubresourceData subResource = new()
        {
            Data = (IntPtr)pixels,
            RowPitch = (nint)textureDescription.Width * 4,
            SlicePitch = 0
        };
        #endregion


        #region // Create Sampler
        // Set the properties for the sampler state.
        SamplerDescription samplerDescription = new()
        {
            Filter = Filter.Anisotropic, // Use anisotropic filtering for smoother sampling.
            AddressU = TextureAddressMode.Mirror,
            AddressV = TextureAddressMode.Mirror,
            AddressW = TextureAddressMode.Mirror,
            ComparisonFunction = ComparisonFunction.Never, // Not needed for standard texture sampling.
            MaxAnisotropy = 16,
            MinLOD = 0,
            MaxLOD = float.MaxValue,
        };

        _sampler = Renderer.Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Flags = DescriptorHeapFlags.ShaderVisible,
            Type = DescriptorHeapType.Sampler
        });
        _sampler.Name = texture.Name + " Sampler";
        // Create the sampler state using the sampler description.
        Renderer.Device.CreateSampler(ref samplerDescription, _sampler.GetCPUDescriptorHandleForHeapStart());

        size = Renderer.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        _sampler.GetCPUDescriptorHandleForHeapStart().Offset(size);
        #endregion
    }
}

unsafe public sealed partial class GUIRenderer
{
    private void CreateBuffers(ImDrawDataPtr data)
    {
        if (_vertexBuffer == null || _vertexBufferSize < data.TotalVtxCount)
        {
            _vertexBuffer?.Release();

            _vertexBufferSize = data.TotalVtxCount + 5000;

            _vertexBuffer = Renderer.Device.CreateCommittedResource(
                HeapType.Upload,
                ResourceDescription.Buffer(_vertexBufferSize * sizeof(ImDrawVert)),
                ResourceStates.GenericRead);
            _vertexBuffer.Name = "ImGui VertexBuffer";
        }

        if (_indexBuffer == null || _indexBufferSize < data.TotalIdxCount)
        {
            _indexBuffer?.Release();

            _indexBufferSize = data.TotalIdxCount + 10000;

            _vertexBuffer = Renderer.Device.CreateCommittedResource(
                HeapType.Upload,
                ResourceDescription.Buffer(_vertexBufferSize * sizeof(ImDrawIdx)),
                ResourceStates.GenericRead);
            _vertexBuffer.Name = "ImGui IndexBuffer";
        }
    }

    private void UpdateBuffers(ImDrawDataPtr data)
    {
        void* pointer;
        _vertexBuffer.Map(0, &pointer);
        var vertexResourcePointer = (ImDrawVert*)new nint(pointer);

        void* pointer2;
        _indexBuffer.Map(0, &pointer2);
        var indexResourcePointer = (ImDrawIdx*)new nint(pointer2);

        // Upload vertex/index data into a single contiguous GPU buffer
        for (int n = 0; n < data.CmdListsCount; n++)
        {
            var cmdlList = data.CmdListsRange[n];

            var vertBytes = cmdlList.VtxBuffer.Size * sizeof(ImDrawVert);
            Buffer.MemoryCopy((void*)cmdlList.VtxBuffer.Data, vertexResourcePointer, vertBytes, vertBytes);

            var idxBytes = cmdlList.IdxBuffer.Size * sizeof(ImDrawIdx);
            Buffer.MemoryCopy((void*)cmdlList.IdxBuffer.Data, indexResourcePointer, idxBytes, idxBytes);

            vertexResourcePointer += cmdlList.VtxBuffer.Size;
            indexResourcePointer += cmdlList.IdxBuffer.Size;
        }

        _vertexBuffer.Unmap(0);
        _indexBuffer.Unmap(0);
    }
}

unsafe public sealed partial class GUIRenderer
{
    private void CreateRootSignature()
    {
        // Create a root signature
        RootSignatureFlags rootSignatureFlags = RootSignatureFlags.AllowInputAssemblerInputLayout;

        RootSignatureDescription1 rootSignatureDescription = new(rootSignatureFlags);

        RootParameter1 texture = new(
            RootParameterType.ShaderResourceView,
            new RootDescriptor1(0, 0),
            ShaderVisibility.All);

        RootParameter1 sampler = new(
            new RootDescriptorTable1(new DescriptorRange1(
                DescriptorRangeType.Sampler, 1, 0, 0, 0)),
            ShaderVisibility.All);

        RootParameter1 constantbuffer = new(
            new RootDescriptorTable1(new DescriptorRange1(
                DescriptorRangeType.ConstantBufferView, 1, 0, 0, 0)),
            ShaderVisibility.All);

        // Define the root parameters
        RootParameter1[] rootParameters = new[] { texture, sampler, constantbuffer };
        rootSignatureDescription.Parameters = rootParameters;

        RootSignature = Renderer.Device.CreateRootSignature(rootSignatureDescription);
        RootSignature.Name = "ImGui RootSignature";
    }

    private void CreateInputLayout(out InputLayoutDescription inputLayoutDescription)
    {
        inputLayoutDescription = new InputLayoutDescription(
            new InputElementDescription("POSITION", 0, Format.R32G32_Float, 0, 0),
            new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 8, 0),
            new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, 16, 0));
    }

    private void CreateShaderByteCode(string shaderFilePath, out ReadOnlyMemory<byte> vertexShaderByteCode, out ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        vertexShaderByteCode = CompileBytecode(DxcShaderStage.Vertex, Paths.SHADERS + shaderFilePath, "VSMain");

        pixelShaderByteCode = CompileBytecode(DxcShaderStage.Pixel, Paths.SHADERS + shaderFilePath, "PSMain");
    }

    private void CreateGraphicsPipelineState(InputLayoutDescription inputLayoutDescription, ReadOnlyMemory<byte> vertexShaderByteCode, ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        GraphicsPipelineStateDescription graphicsPipelineStateDescription = new()
        {
            RootSignature = RootSignature,
            VertexShader = vertexShaderByteCode,
            PixelShader = pixelShaderByteCode,
            InputLayout = inputLayoutDescription,
            SampleMask = int.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            RasterizerState = RasterizerDescription.CullNone,
            BlendState = BlendDescription.Opaque,
            DepthStencilState = DepthStencilDescription.Default,
            DepthStencilFormat = Format.D32_Float,
            SampleDescription = SampleDescription.Default,
            Flags = PipelineStateFlags.None,
            StreamOutput = new StreamOutputDescription(),
        };

        PipelineState = Renderer.Device.CreateGraphicsPipelineState(graphicsPipelineStateDescription);
        PipelineState.Name = "ImGui GraphicsPipelineStateObject";
    }

    private void CreateCommandAllocator()
    {
        CommandAllocator = Renderer.Device.CreateCommandAllocator(CommandListType.Direct);
        CommandAllocator.Name = "ImGui CommandAllocator";
    }

    private void CreateCommandList()
    {
        CommandList = Renderer.Device.CreateCommandList<ID3D12GraphicsCommandList4>(
            CommandListType.Direct,
            CommandAllocator,
            PipelineState);
        CommandList.Name = "ImGui CommandList";
    }

    private ReadOnlyMemory<byte> CompileBytecode(DxcShaderStage stage, string filePath, string entryPoint)
    {
        string directory = Path.GetDirectoryName(filePath);
        string shaderSource = File.ReadAllText(filePath);

        using (ShaderIncludeHandler includeHandler = new(Paths.SHADERS, directory))
        {
            using IDxcResult results = DxcCompiler.Compile(stage, shaderSource, entryPoint, includeHandler: includeHandler);
            if (results.GetStatus().Failure)
                throw new Exception(results.GetErrors());

            return results.GetObjectBytecodeMemory();
        }
    }
}