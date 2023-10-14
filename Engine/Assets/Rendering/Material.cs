using System.Runtime.CompilerServices;

using Vortice.D3DCompiler;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace Engine.Rendering;

public sealed class Material
{
    public static Material CurrentMaterialOnGPU { get; set; }

    public MaterialBuffer MaterialBuffer { get; set; } = new();

    private Renderer _renderer => Renderer.Instance;

    private ID3D11VertexShader _vertexShader;
    private ID3D11PixelShader _pixelShader;

    private ID3D11InputLayout _inputLayout;

    private ID3D11ShaderResourceView _resourceView;
    private ID3D11SamplerState _samplerState;

    public Material(string shaderFilePath, string imageFileName = "Default.png")
    {
        #region // Create MaterialBuffer
        MaterialBuffer.CreatePerModelConstantBuffer();
        #endregion

        #region // Create VertexShader
        if (string.IsNullOrEmpty(shaderFilePath))
            return;

        // Compile the vertex shader bytecode from the specified shader file name.
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(shaderFilePath, "VS", "vs_5_0");
        
        // Create the vertex shader using the compiled bytecode.
        _vertexShader = _renderer.Device.CreateVertexShader(vertexShaderByteCode.Span);
        #endregion

        #region // Create InputLayout
        // Define the input layout for the vertex buffer.
        InputElementDescription[] inputElements = new[] {
                new InputElementDescription("POSITION", 0, Format.R32G32B32_Float, 0, 0), // Position element.
                new InputElementDescription("TEXCOORD", 0, Format.R32G32_Float, InputElementDescription.AppendAligned, 0), // Texture coordinate element.
                new InputElementDescription("NORMAL", 0, Format.R32G32B32_Float, InputElementDescription.AppendAligned, 0)}; // Normal element.

        _inputLayout = _renderer.Device.CreateInputLayout(inputElements, vertexShaderByteCode.Span);
        #endregion

        #region // Create PixelShader 
        // Compile the bytecode for the pixel shader using the specified shader file name and target profile.
        ReadOnlyMemory<byte> pixelShaderByteCode = CompileBytecode(shaderFilePath, "PS", "ps_5_0");

        // Create the pixel shader using the compiled bytecode.
        _pixelShader = _renderer.Device.CreatePixelShader(pixelShaderByteCode.Span);
        #endregion

        #region // Create Texture and Sampler
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

    public void Setup()
    {
        // Set input layout, vertex shader, and pixel shader in the device context.
        // Set the shader resource and sampler in the pixel shader stage of the device context.
        _renderer.Data.SetupMaterial(_inputLayout, _vertexShader, _pixelShader);
        _renderer.Data.SetSamplerState(0, _samplerState);
        _renderer.Data.SetResourceView(0, _resourceView);

        // Assign material to the static variable.
        CurrentMaterialOnGPU = this;
    }

    public void UpdateVertexShader(string shaderFilePath)
    {
        // Compile the vertex shader bytecode from the specified shader file name.
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(shaderFilePath, "VS", "vs_4_0");

        // Create the vertex shader using the compiled bytecode.
        _vertexShader = _renderer.Device.CreateVertexShader(vertexShaderByteCode.Span);
    }

    public void UpdatePixelShader(string shaderFilePath)
    {
        // Compile the vertex shader bytecode from the specified shader file name.
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(shaderFilePath, "PS", "ps_4_0");

        // Create the vertex shader using the compiled bytecode.
        _vertexShader = _renderer.Device.CreateVertexShader(vertexShaderByteCode.Span);
    }

    public void Dispose()
    {
        MaterialBuffer?.Dispose();
        _vertexShader?.Dispose();
        _pixelShader?.Dispose();
        _inputLayout?.Dispose();
        _samplerState?.Dispose();
    }

    public static ReadOnlyMemory<byte> CompileBytecode(string shaderFilePath, string entryPoint, string profile)
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
