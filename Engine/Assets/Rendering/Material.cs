using System.IO;
using System.Runtime.InteropServices;

using SharpGen.Runtime;
using Vortice.Direct3D12;
using Vortice.Dxc;
using Vortice.DXGI;

namespace Engine.Rendering;

public sealed partial class Material
{
    public static Material CurrentMaterialOnGPU { get; set; }

    public MaterialBuffer MaterialBuffer { get; set; } = new();

    public ID3D12RootSignature RootSignature;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList4 CommandList;
    public ID3D12PipelineState PipelineState;

    private ID3D12DescriptorHeap _resourceView;
    private ID3D12DescriptorHeap _samplerState;

    private Renderer _renderer => Renderer.Instance;

    public Material(string shaderFilePath, string imageFileName = "Default.png")
    {
        if (!File.Exists(shaderFilePath))
            return;


        MaterialBuffer.CreatePerModelConstantBuffer();

        CreateRootSignature(shaderFilePath);

        UpdateShader(shaderFilePath);

        #region // Create Texture
        // Load the texture and create a shader resource view for it.
        Loader.ImageLoader.LoadTexture(out var texture, _renderer.Device, imageFileName);

        ShaderResourceViewDescription shaderResourceViewDescription = new()
        {
            Format = texture.Description.Format,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView { MipLevels = texture.Description.MipLevels },
            Shader4ComponentMapping = ShaderComponentMapping.Default
        };

        _resourceView = _renderer.Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            Flags = DescriptorHeapFlags.ShaderVisible
        });
        _renderer.Device.CreateShaderResourceView(texture, shaderResourceViewDescription, _resourceView.GetCPUDescriptorHandleForHeapStart());
        #endregion

        Result result = _renderer.Device.DeviceRemovedReason;
        if (result.Failure)
            throw new Exception(result.Description);

        #region // Create Sampler
        _samplerState = _renderer.Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Type = DescriptorHeapType.Sampler,
            Flags = DescriptorHeapFlags.ShaderVisible
        });

        // Set the properties for the sampler state.
        SamplerDescription samplerStateDescription = new()
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
        // Create the sampler state using the sampler description.
        _renderer.Device.CreateSampler(ref samplerStateDescription, _samplerState.GetCPUDescriptorHandleForHeapStart());
        #endregion
    }

    public void UpdateShader(string shaderFilePath)
    {
        CreateInputLayout(out var inputLayoutDescription);

        CreateShaderByteCode(shaderFilePath, out var vertexShaderByteCode, out var pixelShaderByteCode);

        CreateGraphicsPipelineStateDescription(inputLayoutDescription, vertexShaderByteCode, pixelShaderByteCode);
    }

    public void Setup()
    {
        _renderer.Data.Material.Reset();
        _renderer.Data.Material = this;

        // Set root signature.
        CommandList.SetGraphicsRootSignature(RootSignature);
        // Set shader resource and sampler.
        CommandList.SetGraphicsRootDescriptorTable(0, _resourceView.GetGPUDescriptorHandleForHeapStart());
        CommandList.SetGraphicsRootDescriptorTable(0, _samplerState.GetGPUDescriptorHandleForHeapStart());

        // Set current command list in the graphics queue.
        _renderer.Data.GraphicsQueue.ExecuteCommandList(CommandList);

        // Assign material to the static variable.
        CurrentMaterialOnGPU = this;
    }

    public void Reset()
    {
        CommandAllocator.Reset();
        CommandList.Reset(CommandAllocator, PipelineState);
    }

    public void Dispose()
    {
        MaterialBuffer?.Dispose();

        CommandAllocator?.Dispose();
        CommandList?.Dispose();

        RootSignature?.Dispose();
        PipelineState?.Dispose();
        _resourceView?.Dispose();
        _samplerState?.Dispose();
    }
}

public sealed partial class Material
{
    private static int _count = 0;

    private void CreateRootSignature(string shaderFilePath)
    {
        RootSignatureFlags rootSignatureFlags = RootSignatureFlags.AllowInputAssemblerInputLayout
            | RootSignatureFlags.DenyHullShaderRootAccess
            | RootSignatureFlags.DenyDomainShaderRootAccess
            | RootSignatureFlags.DenyGeometryShaderRootAccess
            | RootSignatureFlags.DenyAmplificationShaderRootAccess
            | RootSignatureFlags.DenyMeshShaderRootAccess;

        RootSignatureDescription1 rootSignatureDesc = new(rootSignatureFlags);

        RootSignature = _renderer.Device.CreateRootSignature(rootSignatureDesc);
        RootSignature.Name = new FileInfo(shaderFilePath).Name + $"_{_count++}";
    }

    private void CreateInputLayout(out InputLayoutDescription inputLayoutDescription)
    {
        inputLayoutDescription = new(
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0), // Position element.
            new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0), // Texture coordinate element.
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0)); // Normal element.
    }

    private void CreateShaderByteCode(string shaderFilePath, out ReadOnlyMemory<byte> vertexShaderByteCode, out ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        // Compile the vertex shader bytecode from the specified shader file name and target profile.
        vertexShaderByteCode = RenderData.CompileBytecode(DxcShaderStage.Vertex, shaderFilePath, "VS");

        // Compile the pixel shader bytecode from the specified shader file name and target profile.
        pixelShaderByteCode = RenderData.CompileBytecode(DxcShaderStage.Pixel, shaderFilePath, "PS");
    }

    private void CreateGraphicsPipelineStateDescription(InputLayoutDescription inputLayoutDescription, ReadOnlyMemory<byte> vertexShaderByteCode, ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        CreateCommandAllocator();

        bool usePSOStream = true;
        if (usePSOStream)
        {
            D3D12GraphicsDevice.PipelineStateStream pipelineStateStream = new()
            {
                RootSignature = RootSignature,
                VertexShader = vertexShaderByteCode.Span,
                PixelShader = pixelShaderByteCode.Span,
                InputLayout = inputLayoutDescription,
                SampleMask = uint.MaxValue,
                PrimitiveTopology = PrimitiveTopologyType.Triangle,
                RasterizerState = _renderer.Data.RasterizerDescription,
                BlendState = BlendDescription.Opaque,
                DepthStencilState = DepthStencilDescription.Default,
                RenderTargetFormats = new[] { RenderData.RenderTargetFormat },
                DepthStencilFormat = RenderData.DepthStencilFormat,
                SampleDescription = SampleDescription.Default
            };

            PipelineState = _renderer.Device.CreatePipelineState(pipelineStateStream);
        }
        else
        {
            GraphicsPipelineStateDescription graphicsPipelineStateDescription = new()
            {
                RootSignature = RootSignature,
                VertexShader = vertexShaderByteCode,
                PixelShader = pixelShaderByteCode,
                InputLayout = inputLayoutDescription,
                SampleMask = uint.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                RasterizerState = _renderer.Data.RasterizerDescription,
                BlendState = BlendDescription.Opaque,
                DepthStencilState = DepthStencilDescription.Default,
                RenderTargetFormats = new[] { RenderData.RenderTargetFormat },
                DepthStencilFormat = RenderData.DepthStencilFormat,
                SampleDescription = SampleDescription.Default
            };

            PipelineState = _renderer.Device.CreateGraphicsPipelineState(graphicsPipelineStateDescription);
        }

        CreateCommandList();
    }

    private void CreateCommandAllocator() =>
        CommandAllocator = _renderer.Device.CreateCommandAllocator(CommandListType.Direct);

    private void CreateCommandList()
    {
        CommandList = _renderer.Device.CreateCommandList<ID3D12GraphicsCommandList4>(
            CommandListType.Direct,
            CommandAllocator,
            PipelineState);

        CommandList.Close();
    }
}

public sealed partial class D3D12GraphicsDevice
{
    [StructLayout(LayoutKind.Sequential)]
    public struct PipelineStateStream
    {
        public PipelineStateSubObjectTypeRootSignature RootSignature;
        public PipelineStateSubObjectTypeVertexShader VertexShader;
        public PipelineStateSubObjectTypePixelShader PixelShader;
        public PipelineStateSubObjectTypeInputLayout InputLayout;
        public PipelineStateSubObjectTypeSampleMask SampleMask;
        public PipelineStateSubObjectTypePrimitiveTopology PrimitiveTopology;
        public PipelineStateSubObjectTypeRasterizer RasterizerState;
        public PipelineStateSubObjectTypeBlend BlendState;
        public PipelineStateSubObjectTypeDepthStencil DepthStencilState;
        public PipelineStateSubObjectTypeRenderTargetFormats RenderTargetFormats;
        public PipelineStateSubObjectTypeDepthStencilFormat DepthStencilFormat;
        public PipelineStateSubObjectTypeSampleDescription SampleDescription;
    }
}