using Vortice.Direct3D11;
using Vortice.D3DCompiler;

namespace Engine.Rendering;

public class ComputeShader
{
    private Renderer _renderer => Renderer.Instance;

    private ID3D11ComputeShader _postProcessComputeShader;

    public ComputeShader(string computeShaderFilePath)
    {
        if (string.IsNullOrEmpty(computeShaderFilePath))
            return;

        // Compile the compute shader bytecode from the specified compute shader file name.
        ReadOnlyMemory<byte> vertexShaderByteCode = CompileBytecode(computeShaderFilePath, "CS", "cs_5_0");

        // Create the compute shader using the compiled bytecode.
        _postProcessComputeShader = _renderer.Device.CreateComputeShader(vertexShaderByteCode.Span);
    }

    public void UpdateResource() { }

    public void SetBuffer() { }
    public void SetConstantBuffer() { }
    public void SetFloat(string name, float value) { }
    public void SetFloats() { }
    public void SetInt() { }
    public void SetInts() { }
    public void SetMatrix() { }

    public void SetTexture(int kernelIndex, int nameID, object Texture, int mipLevel) { }

    public int FindKernel(string kernelName) { return 0; }

    public void Dispatch(int kernelIndex, int threadGroupsX, int threadGroupsY, int threadGroupsZ) { }

    public void Result() { }

    public void Dispose()
    {
        _postProcessComputeShader?.Dispose();
    }

    public static ReadOnlyMemory<byte> CompileBytecode(string computeShaderFilePath, string entryPoint, string profile)
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
        return Compiler.CompileFromFile(computeShaderFilePath, entryPoint, profile, shaderFlags);
    }
}
