using System.Collections.Generic;
using System.Drawing;
using System.IO;

using Vortice.Direct3D12;
using Vortice.Dxc;
using Vortice.DXGI;

using ImDrawIdx = System.UInt16;

namespace Engine.GUI;

unsafe public sealed partial class GUIRenderer
{
    private ID3D12GraphicsCommandList _commandList;
    private ID3D12CommandAllocator _commandAllocator;

    private List<ID3D12Resource> _vertexBuffers = new List<ID3D12Resource>();
    private List<ID3D12Resource> _indexBuffers = new List<ID3D12Resource>();

    private ID3D12RootSignature _rootSignature;
    private ID3D12PipelineState _pipelineState;

    private ID3D12Resource _viewConstantBuffer;
    private IntPtr _viewConstantBufferPointer;

    public Renderer Renderer => _renderer ??= Renderer.Instance;
    private Renderer _renderer;

    public GUIRenderer()
    {
        var io = ImGui.GetIO();
        io.BackendFlags |= ImGuiBackendFlags.RendererHasVtxOffset;  // We can honor the ImDrawCmd::VtxOffset field, allowing for large meshes.

        _commandAllocator = Renderer.Device.CreateCommandAllocator(CommandListType.Direct);
        _commandList = Renderer.Device.CreateCommandList<ID3D12GraphicsCommandList>(0, CommandListType.Direct, _commandAllocator, null);

        CreateRootSignature();
        CreatePipelineState();
    }

    public void Update(IntPtr imGuiContext, Size newSize)
    {
        ImGui.SetCurrentContext(imGuiContext);
        var io = ImGui.GetIO();

        io.DeltaTime = Time.DeltaF;
        io.DisplaySize = newSize.ToVector2();

        ImGui.NewFrame();
    }

    public void Render() =>
        Draw(ImGui.GetDrawData());

    private void Draw(ImDrawDataPtr data)
    {
        // Record rendering commands into a DirectX 12 command list
        _commandAllocator.Reset();
        _commandList.Reset(_commandAllocator, _pipelineState); // Use the pipeline state

        // Avoid rendering when minimized
        if (data.DisplaySize.X <= 0.0f || data.DisplaySize.Y <= 0.0f)
            return;

        // Create and initialize view constant buffer
        CreateViewConstantBuffer(data);

        // Set root signature
        _commandList.SetGraphicsRootSignature(_rootSignature);

        // Set view constant buffer
        _commandList.SetGraphicsRootConstantBufferView(0, _viewConstantBuffer.GPUVirtualAddress);

        // Render ImGui draw data
        RenderImDrawData(data);

        // Close and execute the command list
        _commandList.Close();
        Renderer.Data.GraphicsQueue.ExecuteCommandList(_commandList);
    }

    private void RenderImDrawData(ImDrawDataPtr data)
    {
        // Record ImGui draw commands
        for (int n = 0; n < data.CmdListsCount; n++)
        {
            ImDrawListPtr cmdList = data.CmdListsRange[n];
            int vtxBufferSize = cmdList.VtxBuffer.Size * sizeof(ImDrawVert);
            int idxBufferSize = cmdList.IdxBuffer.Size * sizeof(ImDrawIdx);

            // Update vertex buffer
            ID3D12Resource vertexBuffer = _vertexBuffers[n];
            ImDrawVert* vertexData = vertexBuffer.Map<ImDrawVert>(0);
            Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vertexData, vtxBufferSize, vtxBufferSize);
            vertexBuffer.Unmap(0);

            // Update index buffer
            ID3D12Resource indexBuffer = _indexBuffers[n];
            ImDrawIdx* indexData = indexBuffer.Map<ImDrawIdx>(0);
            Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, indexData, idxBufferSize, idxBufferSize);
            indexBuffer.Unmap(0);

            // Set vertex and index buffer
            _commandList.IASetVertexBuffers(0, new VertexBufferView(vertexBuffer.GPUVirtualAddress, vtxBufferSize, sizeof(ImDrawVert)));
            _commandList.IASetIndexBuffer(new IndexBufferView(indexBuffer.GPUVirtualAddress, idxBufferSize, sizeof(ImDrawIdx) == 2));

            // Render command lists
            for (int cmdIndex = 0; cmdIndex < cmdList.CmdBuffer.Size; cmdIndex++)
            {
                ImDrawCmdPtr cmd = cmdList.CmdBuffer[cmdIndex];

                // Set render target
                _commandList.OMSetRenderTargets(Renderer.Data.BufferRenderTargetView.GetCPUDescriptorHandleForHeapStart(), null);

                // Render the command
                _commandList.DrawIndexedInstanced((int)cmd.ElemCount, 1, (int)cmd.IdxOffset, (int)cmd.VtxOffset, 0);
            }
        }
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

        _rootSignature = Renderer.Device.CreateRootSignature(rootSignatureDescription);
        _rootSignature.Name = "ImGui Root Signature";
    }

    private void CreatePipelineState()
    {
        // Load and compile shaders (VS and PS)
        var vertexShaderByteCode = CompileBytecode(DxcShaderStage.Vertex, Paths.SHADERS + "ImGui.hlsl", "VSMain");
        var pixelShaderByteCode = CompileBytecode(DxcShaderStage.Pixel, Paths.SHADERS + "ImGui.hlsl", "PSMain");

        // Define input layout
        var inputLayoutDescription = new InputLayoutDescription(
            new InputElementDescription("POSITION", 0, Format.R32G32_Float, 0, 0),
            new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 8, 0),
            new InputElementDescription("COLOR", 0, Format.R8G8B8A8_UNorm, 16, 0));

        // Describe and create the graphics pipeline state object (PSO)
        var psoDesc = new GraphicsPipelineStateDescription
        {
            InputLayout = inputLayoutDescription,
            RootSignature = _rootSignature,
            VertexShader = vertexShaderByteCode,
            PixelShader = pixelShaderByteCode,
            RasterizerState = RasterizerDescription.CullNone,
            BlendState = BlendDescription.Opaque,
            DepthStencilFormat = Format.D32_Float,
            DepthStencilState = DepthStencilDescription.Default,
            SampleMask = int.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            Flags = PipelineStateFlags.None,
            SampleDescription = SampleDescription.Default,
            StreamOutput = new StreamOutputDescription(),
        };

        _pipelineState = Renderer.Device.CreateGraphicsPipelineState(psoDesc);
        _pipelineState.Name = "ImGui Pipeline State Object";
    }

    private void CreateViewConstantBuffer(ImDrawDataPtr data)
    {
        //Create View Constant Buffer.
        _viewConstantBuffer = Renderer.Device.CreateCommittedResource(
            HeapType.Upload,
            ResourceDescription.Buffer(sizeof(ViewConstantBuffer)),
            ResourceStates.GenericRead);
        _viewConstantBuffer.Name = "ImGui View ConstantBuffer";

        float L = data.DisplayPos.X;
        float R = data.DisplayPos.X + data.DisplaySize.X;
        float T = data.DisplayPos.Y;
        float B = data.DisplayPos.Y + data.DisplaySize.Y;
        Matrix4x4 mvp = new(
            2.0f/(R-L),     0.0f,           0.0f,   0.0f,
            0.0f,           2.0f/(T-B),     0.0f,   0.0f,
            0.0f,           0.0f,           0.5f,   0.0f,
            (R+L)/(L-R),    (T+B)/(B-T),    0.5f,   1.0f);

        // Map the buffer and store the pointer for later use
        var viewConstantBufferPointer = _viewConstantBuffer.Map<ViewConstantBuffer>(0);
        *viewConstantBufferPointer = new ViewConstantBuffer(mvp, Vector3.Zero);
        _viewConstantBuffer.Unmap(0);
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
