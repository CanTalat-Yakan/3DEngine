﻿using Vortice.Direct3D12;
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
        MaterialBuffer.CreatePerModelConstantBuffer();

        CreateRootSignature();

        UpdateShader(shaderFilePath);

        #region // Create Texture
        // Load the texture and create a shader resource view for it.
        var texture = Loader.ImageLoader.LoadTexture(_renderer.Device, imageFileName); // my own function. What should it return? A ID3D12Resource?

        _resourceView = _renderer.Device.CreateDescriptorHeap(new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView,
            DescriptorCount = 1, // The number of descriptors you need
            Flags = DescriptorHeapFlags.ShaderVisible, // This makes the heap shader visible
        });

        ShaderResourceViewDescription shaderResourceViewDescription = new()
        {
            Format = texture.Description.Format, // The format of the texture.
            ViewDimension = ShaderResourceViewDimension.Texture2D, // Texture dimension.
            Texture2D = new Texture2DShaderResourceView { MipLevels = texture.Description.MipLevels }
        };
        _renderer.Device.CreateShaderResourceView(texture, shaderResourceViewDescription, _resourceView.GetCPUDescriptorHandleForHeapStart());
        #endregion

        #region // Create Sampler
        _samplerState = _renderer.Device.CreateDescriptorHeap(new DescriptorHeapDescription
        {
            Type = DescriptorHeapType.Sampler,
            DescriptorCount = 1, // The number of descriptors you need
            Flags = DescriptorHeapFlags.ShaderVisible, // This makes the heap shader visible
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
        if (string.IsNullOrEmpty(shaderFilePath))
            return;

        CreateInputLayout(out var inputElementDescription);

        CreateShaderByteCode(shaderFilePath, out var vertexShaderByteCode, out var pixelShaderByteCode);

        _pipelineState = CreateGraphicsPipelineStateDescription(inputElementDescription, vertexShaderByteCode, pixelShaderByteCode);
    }

    public void Setup()
    {
        // Set necessary state.
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
    private void CreateRootSignature()
    {
        RootSignatureFlags rootSignatureFlags = RootSignatureFlags.AllowInputAssemblerInputLayout 
            | RootSignatureFlags.DenyHullShaderRootAccess
            | RootSignatureFlags.DenyDomainShaderRootAccess
            | RootSignatureFlags.DenyGeometryShaderRootAccess
            | RootSignatureFlags.DenyAmplificationShaderRootAccess
            | RootSignatureFlags.DenyMeshShaderRootAccess;

        RootSignatureDescription1 rootSignatureDesc = new(rootSignatureFlags);
        _rootSignature = _renderer.Device.CreateRootSignature(rootSignatureDesc);
    }

    private ID3D12PipelineState CreateGraphicsPipelineStateDescription(InputElementDescription[] inputElementDescription, ReadOnlyMemory<byte> vertexShaderByteCode, ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        GraphicsPipelineStateDescription pipelineStateObjectDescription = new()
        {
            RootSignature = _rootSignature,
            VertexShader = vertexShaderByteCode,
            PixelShader = pixelShaderByteCode,
            InputLayout = new(inputElementDescription),
            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            RasterizerState = _renderer.Data.RasterizerState,
            BlendState = BlendDescription.Opaque,
            DepthStencilState = DepthStencilDescription.Default,
            RenderTargetFormats = new[] { RenderData.RenderTargetFormat },
            DepthStencilFormat = RenderData.DepthStencilFormat,
            SampleDescription = new(_renderer.Config.SupportedSampleCount, _renderer.Config.QualityLevels)
        };

        return _renderer.Device.CreateGraphicsPipelineState(pipelineStateObjectDescription);
    }

    private void CreateInputLayout(out InputElementDescription[] inputElementDescription)
    {
        inputElementDescription = new[]
        {
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0), // Position element.
            new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0), // Texture coordinate element.
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0) // Normal element.
        };
    }

    private void CreateShaderByteCode(string shaderFilePath, out ReadOnlyMemory<byte> vertexShaderByteCode, out ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        // Compile the vertex shader bytecode from the specified shader file name and target profile.
        vertexShaderByteCode = RenderData.CompileBytecode(shaderFilePath, "VS", "vs_5_0");

        // Compile the pixel shader bytecode from the specified shader file name and target profile.
        pixelShaderByteCode = RenderData.CompileBytecode(shaderFilePath, "PS", "ps_5_0");
    }
}
