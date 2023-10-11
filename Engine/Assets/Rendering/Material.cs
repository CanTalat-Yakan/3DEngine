using System.Runtime.CompilerServices;

using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Engine.Rendering;

public sealed class Material
{
    public static Material CurrentMaterialOnGPU { get; private set; }

    public MaterialBuffer MaterialBuffer { get; set; }

    private Renderer _renderer => Renderer.Instance;

    private ID3D11VertexShader _vertexShader;
    private ID3D11PixelShader _pixelShader;

    private ID3D11InputLayout _inputLayout;

    private ID3D11ShaderResourceView _resourceView;
    private ID3D11SamplerState _samplerState;

    private ID3D11Buffer _model;

    public Material(string shaderFilePath, string imageFileName = null)
    {
        #region //Create VertexShader
        if (string.IsNullOrEmpty(shaderFilePath))
        {
            new Exception("ShaderFilePath in the Material Constructor was null");
            return;
        }

        // Compile the vertex shader bytecode from the specified shader file name.
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(shaderFilePath, "VS", "vs_5_0");
        
        // Create the vertex shader using the compiled bytecode.
        _vertexShader = _renderer.Device.CreateVertexShader(vertexShaderByteCode.Span);
        #endregion

        #region //Create InputLayout
        // Define the input layout for the vertex buffer.
        InputElementDescription[] inputElements = new[] {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0), // Position element.
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0), // Texture coordinate element.
                new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0)}; // Normal element.

        _inputLayout = _renderer.Device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);
        #endregion

        #region //Create PixelShader 
        // Compile the bytecode for the pixel shader using the specified shader file name and target profile.
        ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode(shaderFilePath, "PS", "ps_5_0");

        // Create the pixel shader using the compiled bytecode.
        _pixelShader = _renderer.Device.CreatePixelShader(pixelShaderByteCode.Span);
        #endregion

        #region //Create ConstantBuffer for Model
        // Create the constant buffer for model-related data.
        PerModelConstantBuffer cbModel = new();

        // Set up the description for the constant buffer.
        BufferDescription bufferDescription = new()
        {
            BindFlags = BindFlags.ConstantBuffer,
            CPUAccessFlags = CpuAccessFlags.Write,
            Usage = ResourceUsage.Dynamic,
        };

        // Create the constant buffer with the given description.
        _model = _renderer.Device.CreateBuffer(cbModel, bufferDescription);
        #endregion

        #region //Create Texture and Sampler
        if (string.IsNullOrEmpty(imageFileName))
            return;

        // Load the texture and create a shader resource view for it.
        var texture = Loader.ImageLoader.LoadTexture(_renderer.Device, imageFileName);
        _resourceView = _renderer.Device.CreateShaderResourceView(texture);

        // Set the properties for the sampler state.
        SamplerDescription samplerStateDescription = new()
        {
            Filter = Filter.MinMagMipLinear,
            AddressU = TextureAddressMode.Clamp,
            AddressV = TextureAddressMode.Clamp,
            AddressW = TextureAddressMode.Clamp,
            ComparisonFunc = ComparisonFunction.Always,
            MaxAnisotropy = 16,
            MinLOD = 0,
            MaxLOD = float.MaxValue,
        };

        // Create the sampler state using the sampler state description.
        _samplerState = _renderer.Device.CreateSamplerState(samplerStateDescription);
        #endregion
    }

    internal void Set()
    {
        // Set input layout, vertex shader, and pixel shader in the device context.
        // Set the shader resource and sampler in the pixel shader stage of the device context.
        _renderer.Data.SetupMaterial(_inputLayout, _vertexShader, _pixelShader);
        _renderer.Data.SetSamplerState(0, _samplerState);
        _renderer.Data.SetResourceView(0, _resourceView);

        // Assign material to the static variable.
        CurrentMaterialOnGPU = this;
    }

    internal void UpdateModelConstantBuffer(PerModelConstantBuffer constantBuffer)
    {
        // Map the constant buffer and copy the models model-view matrix into it.
        unsafe
        {
            // Map the constant buffer to memory for write access.
            MappedSubresource mappedResource = _renderer.Data.DeviceContext.Map(_model, MapMode.WriteDiscard);
            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref constantBuffer);
            // Unmap the constant buffer from memory.
            _renderer.Data.DeviceContext.Unmap(_model, 0);
        }

        // Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.SetConstantBufferVS(1, _model);
    }

    internal void UpdateMaterialConstantBuffer() =>
        MaterialBuffer?.UpdateConstantBuffer();

    internal void UpdateVertexShader(string shaderFilePath)
    {
        // Compile the vertex shader bytecode from the specified shader file name.
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(shaderFilePath, "VS", "vs_4_0");

        // Create the vertex shader using the compiled bytecode.
        _vertexShader = _renderer.Device.CreateVertexShader(vertexShaderByteCode.Span);
    }

    internal void UpdatePixelShader(string shaderFilePath)
    {
        // Compile the vertex shader bytecode from the specified shader file name.
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(shaderFilePath, "PS", "ps_4_0");

        // Create the vertex shader using the compiled bytecode.
        _vertexShader = _renderer.Device.CreateVertexShader(vertexShaderByteCode.Span);
    }

    internal void Dispose()
    {
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _inputLayout?.Dispose();
        _samplerState?.Dispose();
        _model?.Dispose();
    }

    internal static ReadOnlyMemory<byte> CompileBytecode(string shaderFilePath, string entryPoint, string profile)
    {
        // Shader flags to enable strictness and set optimization level or debug mode.
        ShaderFlags shaderFlags = ShaderFlags.EnableStrictness;
#if DEBUG
        shaderFlags |= ShaderFlags.Debug;
        shaderFlags |= ShaderFlags.SkipValidation;
#else
        shaderFlags |= ShaderFlags.OptimizationLevel3;
#endif

        // Compile the shader from the specified file using the specified entry point, profile, and flags.
        return Compiler.CompileFromFile(shaderFilePath, entryPoint, profile, shaderFlags);
    }
}
