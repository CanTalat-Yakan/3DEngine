using System.IO;

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

    internal Renderer Renderer => _renderer is not null ? _renderer : _renderer = Renderer.Instance;
    private Renderer _renderer;

    public Material(string shaderFilePath, string imageFileName = "Default.png")
    {
        if (!File.Exists(shaderFilePath))
            return;

        MaterialBuffer.CreatePerModelConstantBuffer();

        CreateRootSignature(shaderFilePath);

        UpdateShader(shaderFilePath);

        CreateTextureAndSampler(imageFileName);
    }

    public void UpdateShader(string shaderFilePath)
    {
        CreateInputLayout(out var inputLayoutDescription);

        CreateShaderByteCode(shaderFilePath, out var vertexShaderByteCode, out var pixelShaderByteCode);

        CreateGraphicsPipelineStateDescription(inputLayoutDescription, vertexShaderByteCode, pixelShaderByteCode);
    }

    public void Setup()
    {
        Renderer.Data.Material?.Reset();
        Renderer.Data.Material = this;

        // Set root signature.
        CommandList.SetGraphicsRootSignature(RootSignature);
        // Set shader resource and sampler.
        CommandList.SetGraphicsRootDescriptorTable(0, _resourceView.GetGPUDescriptorHandleForHeapStart());
        CommandList.SetGraphicsRootDescriptorTable(0, _samplerState.GetGPUDescriptorHandleForHeapStart());

        // Set current command list in the graphics queue.
        Renderer.Data.GraphicsQueue.ExecuteCommandList(CommandList);

        // Assign material to the static variable.
        CurrentMaterialOnGPU = this;
    }

    public void Close() =>
        CommandList.Close();

    public void Reset()
    {
        CommandAllocator.Reset();
        CommandList.Reset(CommandAllocator, PipelineState);
    }

    public void Dispose()
    {
        MaterialBuffer?.Dispose();

        RootSignature?.Dispose();

        CommandAllocator?.Dispose();
        CommandList?.Dispose();
        PipelineState?.Dispose();

        _resourceView?.Dispose();
        _samplerState?.Dispose();
    }

    private void CreateTextureAndSampler(string imageFileName)
    {
        #region // Create Texture
        // Load the texture and create a shader resource view for it.
        Loader.ImageLoader.LoadTexture(out var texture, Renderer.Device, imageFileName);

        ShaderResourceViewDescription shaderResourceViewDescription = new()
        {
            Format = texture.Description.Format,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView { MipLevels = texture.Description.MipLevels },
            Shader4ComponentMapping = ShaderComponentMapping.Default
        };

        _resourceView = Renderer.Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Flags = DescriptorHeapFlags.ShaderVisible,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
        });
        _resourceView.Name = texture.Name + " Texture";
        Renderer.Device.CreateShaderResourceView(texture, shaderResourceViewDescription, _resourceView.GetCPUDescriptorHandleForHeapStart());
        #endregion

        Result result = Renderer.Device.DeviceRemovedReason;
        if (result.Failure)
            throw new Exception(result.Description);

        #region // Create Sampler
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

        _samplerState = Renderer.Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Flags = DescriptorHeapFlags.ShaderVisible,
            Type = DescriptorHeapType.Sampler
        });
        _samplerState.Name = texture.Name + " Sampler";
        // Create the sampler state using the sampler description.
        Renderer.Device.CreateSampler(ref samplerStateDescription, _samplerState.GetCPUDescriptorHandleForHeapStart());
        #endregion
    }
}

public sealed partial class Material
{
    private static int _count = 1;

    private void CreateRootSignature(string shaderFilePath)
    {
        // Create a root signature
        RootSignatureFlags rootSignatureFlags =
              RootSignatureFlags.AllowInputAssemblerInputLayout
            | RootSignatureFlags.ConstantBufferViewShaderResourceViewUnorderedAccessViewHeapDirectlyIndexed
            | RootSignatureFlags.DenyHullShaderRootAccess
            | RootSignatureFlags.DenyDomainShaderRootAccess
            | RootSignatureFlags.DenyGeometryShaderRootAccess
            | RootSignatureFlags.DenyAmplificationShaderRootAccess
            | RootSignatureFlags.DenyMeshShaderRootAccess;

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
                DescriptorRangeType.ConstantBufferView, 3, 0, 0, 0)),
            ShaderVisibility.All);

        // Define the root parameters
        RootParameter1[] rootParameters = new[] { texture, sampler, constantbuffer };
        rootSignatureDescription.Parameters = rootParameters;

        RootSignature = Renderer.Device.CreateRootSignature(rootSignatureDescription);
        RootSignature.Name = new FileInfo(shaderFilePath).Name + " " + _count++;
    }

    private void CreateInputLayout(out InputLayoutDescription inputLayoutDescription)
    {
        inputLayoutDescription = new InputLayoutDescription(
            new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0),
            new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, 12, 0),
            new InputElementDescription("TANGENT", 0, Format.R32G32B32_Float, 24, 0),
            new InputElementDescription("UV", 0, Format.R32G32_Float, 32, 0));
    }

    private void CreateShaderByteCode(string shaderFilePath, out ReadOnlyMemory<byte> vertexShaderByteCode, out ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        // Compile the vertex shader bytecode from the specified shader file name and target profile.
        vertexShaderByteCode = RenderData.CompileBytecode(DxcShaderStage.Vertex, shaderFilePath, "VSMain");

        // Compile the pixel shader bytecode from the specified shader file name and target profile.
        pixelShaderByteCode = RenderData.CompileBytecode(DxcShaderStage.Pixel, shaderFilePath, "PSMain");
    }

    private void CreateGraphicsPipelineStateDescription(InputLayoutDescription inputLayoutDescription, ReadOnlyMemory<byte> vertexShaderByteCode, ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        CreateCommandAllocator();

        GraphicsPipelineStateDescription graphicsPipelineStateDescription = new()
        {
            RootSignature = RootSignature,
            VertexShader = vertexShaderByteCode,
            PixelShader = pixelShaderByteCode,
            InputLayout = inputLayoutDescription,
            SampleMask = uint.MaxValue,
            PrimitiveTopologyType = PrimitiveTopologyType.Triangle,
            RasterizerState = Renderer.Data.RasterizerDescription,
            BlendState = Renderer.Data.BlendDescription,
            DepthStencilState = DepthStencilDescription.Default,
            RenderTargetFormats = new[] { RenderData.RenderTargetFormat },
            DepthStencilFormat = RenderData.DepthStencilFormat,
            SampleDescription = SampleDescription.Default
        };
        PipelineState = Renderer.Device.CreateGraphicsPipelineState(graphicsPipelineStateDescription);
        PipelineState.Name = "GraphicsPipelineStateObject " + _count;

        CreateCommandList();
    }

    private void CreateCommandAllocator()
    {
        CommandAllocator = Renderer.Device.CreateCommandAllocator(CommandListType.Direct);
        CommandAllocator.Name = "CommandAllocator " + _count;
    }

    private void CreateCommandList()
    {
        CommandList = Renderer.Device.CreateCommandList<ID3D12GraphicsCommandList4>(
            CommandListType.Direct,
            CommandAllocator,
            PipelineState);
        CommandList.Name = "CommandList " + _count;
    }
}