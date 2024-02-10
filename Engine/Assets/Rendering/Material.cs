using System.IO;

using Vortice.Direct3D12;
using Vortice.Dxc;
using Vortice.DXGI;

namespace Engine.Rendering;

public sealed partial class Material
{
    public static Material OnGPU { get; set; }

    public MaterialBuffer MaterialBuffer { get; set; } = new();

    public ID3D12RootSignature RootSignature;

    public ID3D12CommandAllocator CommandAllocator;
    public ID3D12GraphicsCommandList4 CommandList;
    public ID3D12PipelineState PipelineState;

    public ID3D12Resource Texture;
    private ID3D12DescriptorHeap _textureView;
    private ID3D12DescriptorHeap _sampler;

    internal Renderer Renderer => _renderer ??= Renderer.Instance;
    private Renderer _renderer;

    public Material(string shaderFilePath, string imageFileName = "Default.png")
    {
        if (!File.Exists(shaderFilePath))
            return;

        MaterialBuffer.CreatePerModelConstantBuffer();

        CreateRootSignature(shaderFilePath);

        UpdateShader(shaderFilePath);

        CreateTextureAndSampler(imageFileName);

        Core.Instance.OnDispose += Dispose;
    }

    public void UpdateShader(string shaderFilePath)
    {
        CreateInputLayout(out var inputLayoutDescription);

        CreateShaderByteCode(shaderFilePath, out var vertexShaderByteCode, out var pixelShaderByteCode);

        CreateGraphicsPipelineState(inputLayoutDescription, vertexShaderByteCode, pixelShaderByteCode);

        CreateCommandAllocator();

        CreateCommandList();
    }

    public void Setup()
    {
        Renderer.Data.Material?.Reset();
        Renderer.Data.Material = this;

        // Set root signature.
        CommandList.SetGraphicsRootSignature(RootSignature);

        //// Set texture by transitioning state.
        //CommandList.ResourceBarrierTransition(Texture, ResourceStates.CopyDest, ResourceStates.UnorderedAccess);
        //CommandList.SetGraphicsRootDescriptorTable(0, _textureView.GetGPUDescriptorHandleForHeapStart());
        //CommandList.ResourceBarrierTransition(Texture, ResourceStates.UnorderedAccess, ResourceStates.CopyDest);

        //// Set sampler description.
        //CommandList.SetGraphicsRootDescriptorTable(0, _samplerState.GetGPUDescriptorHandleForHeapStart());

        Renderer.CheckDeviceRemoved();

        // Set current command list in the graphics queue.
        CommandList.Close();
        Renderer.Data.GraphicsQueue.ExecuteCommandList(CommandList);

        // Assign material to the static variable.
        OnGPU = this;
    }

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

        _textureView?.Dispose();
        _sampler?.Dispose();
    }
}

public sealed partial class Material
{
    private void CreateTextureAndSampler(string imageFileName)
    {
        #region // Create Texture
        // Load the texture and create a shader resource view for it.
        Loader.ImageLoader.LoadTexture(out Texture, Renderer.Device, imageFileName);

        ShaderResourceViewDescription shaderResourceViewDescription = new()
        {
            Format = Texture.Description.Format,
            ViewDimension = ShaderResourceViewDimension.Texture2D,
            Texture2D = new Texture2DShaderResourceView { MipLevels = Texture.Description.MipLevels },
            Shader4ComponentMapping = ShaderComponentMapping.Default
        };

        _textureView = Renderer.Device.CreateDescriptorHeap(new()
        {
            DescriptorCount = 1,
            Flags = DescriptorHeapFlags.ShaderVisible,
            Type = DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView
        });
        _textureView.Name = Texture.Name + " Texture";
        Renderer.Device.CreateShaderResourceView(Texture, shaderResourceViewDescription, _textureView.GetCPUDescriptorHandleForHeapStart());

        var size = Renderer.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        _textureView.GetCPUDescriptorHandleForHeapStart().Offset(size);
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
        _sampler.Name = Texture.Name + " Sampler";
        // Create the sampler state using the sampler description.
        Renderer.Device.CreateSampler(ref samplerDescription, _sampler.GetCPUDescriptorHandleForHeapStart());

        size = Renderer.Device.GetDescriptorHandleIncrementSize(DescriptorHeapType.ConstantBufferViewShaderResourceViewUnorderedAccessView);
        _sampler.GetCPUDescriptorHandleForHeapStart().Offset(size);
        #endregion
    }
}

public sealed partial class Material
{
    private static int _count { get; set; } = 1;

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
                DescriptorRangeType.ConstantBufferView, 2, 0, 0, 0)),
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
            new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, 32, 0));
    }

    private void CreateShaderByteCode(string shaderFilePath, out ReadOnlyMemory<byte> vertexShaderByteCode, out ReadOnlyMemory<byte> pixelShaderByteCode)
    {
        vertexShaderByteCode = CompileBytecode(DxcShaderStage.Vertex, shaderFilePath, "VSMain");

        pixelShaderByteCode = CompileBytecode(DxcShaderStage.Pixel, shaderFilePath, "PSMain");
    }

    private void CreateGraphicsPipelineState(InputLayoutDescription inputLayoutDescription, ReadOnlyMemory<byte> vertexShaderByteCode, ReadOnlyMemory<byte> pixelShaderByteCode)
    {
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
            DepthStencilFormat = RenderData.DepthStencilFormat,
            RenderTargetFormats = new[] { RenderData.RenderTargetFormat },
            SampleDescription = SampleDescription.Default
        };
        PipelineState = Renderer.Device.CreateGraphicsPipelineState(graphicsPipelineStateDescription);
        PipelineState.Name = "GraphicsPipelineStateObject " + _count;
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