using SharpGen.Runtime;
using System.IO;
using System.Runtime.InteropServices;

using Vortice.Direct3D12;
using Vortice.Dxc;
using Vortice.DXGI;

namespace Engine.Rendering;

public sealed partial class Material
{
    public static Material CurrentMaterialOnGPU { get; set; }

    public MaterialBuffer MaterialBuffer { get; set; } = new();

    private Renderer _renderer => Renderer.Instance;

    private ID3D12RootSignature _rootSignature;
    private ID3D12PipelineState _pipelineState;

    private ID3D12DescriptorHeap _resourceView;
    private ID3D12DescriptorHeap _samplerState;

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
        // Set current material root state.
        _renderer.Data.CommandList.SetGraphicsRootSignature(_rootSignature);
        // Set input layout, vertex shader, and pixel shader in the device context.
        _renderer.Data.SetupMaterial(_pipelineState);

        // Set the shader resource and sampler.
        _renderer.Data.CommandList.SetGraphicsRootDescriptorTable(0, _resourceView.GetGPUDescriptorHandleForHeapStart());
        _renderer.Data.CommandList.SetGraphicsRootDescriptorTable(0, _samplerState.GetGPUDescriptorHandleForHeapStart());

        // Assign material to the static variable.
        CurrentMaterialOnGPU = this;
    }

    public void Dispose()
    {
        MaterialBuffer?.Dispose();

        _rootSignature?.Dispose();
        _pipelineState?.Dispose();
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

        _rootSignature = _renderer.Device.CreateRootSignature(rootSignatureDesc);
        _rootSignature.Name = new FileInfo(shaderFilePath).Name + $"_{_count++}";
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
        bool usePSOStream = true;
        if (usePSOStream)
        {
            D3D12GraphicsDevice.PipelineStateStream pipelineStateStream = new()
            {
                RootSignature = _rootSignature,
                VertexShader = vertexShaderByteCode.Span,
                PixelShader = pixelShaderByteCode.Span,
                InputLayout = inputLayoutDescription,
                SampleMask = uint.MaxValue,
                PrimitiveTopology = PrimitiveTopologyType.Triangle,
                RasterizerState = _renderer.Data.RasterizerState,
                BlendState = BlendDescription.Opaque,
                DepthStencilState = DepthStencilDescription.Default,
                RenderTargetFormats = new[] { RenderData.RenderTargetFormat },
                DepthStencilFormat = RenderData.DepthStencilFormat,
                SampleDescription = SampleDescription.Default
            };

            _pipelineState = _renderer.Device.CreatePipelineState(pipelineStateStream);
        }
        else
        {
            GraphicsPipelineStateDescription graphicsPipelineStateDescription = new()
            {
                RootSignature = _rootSignature,
                VertexShader = vertexShaderByteCode,
                PixelShader = pixelShaderByteCode,
                InputLayout = inputLayoutDescription,
                SampleMask = uint.MaxValue,
                PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
                RasterizerState = _renderer.Data.RasterizerState,
                BlendState = BlendDescription.Opaque,
                DepthStencilState = DepthStencilDescription.Default,
                RenderTargetFormats = new[] { RenderData.RenderTargetFormat },
                DepthStencilFormat = RenderData.DepthStencilFormat,
                SampleDescription = SampleDescription.Default
            };

            Result result = _renderer.Device.CreateGraphicsPipelineState(graphicsPipelineStateDescription, out _pipelineState);
            if (result.Failure)
                throw new Exception(result.Description);
        }
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