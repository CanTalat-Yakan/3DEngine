namespace Engine;

/// <summary>Shader abstractions for SPIR-V shader modules.</summary>

/// <summary>Shader pipeline stage.</summary>
public enum ShaderStage
{
    /// <summary>Vertex shader stage - transforms vertex positions.</summary>
    Vertex,
    /// <summary>Fragment (pixel) shader stage - computes final pixel color.</summary>
    Fragment
}

/// <summary>Descriptor for creating a shader module from SPIR-V bytecode.</summary>
/// <param name="Stage">The pipeline stage this shader belongs to.</param>
/// <param name="Bytecode">The compiled SPIR-V bytecode.</param>
/// <param name="EntryPoint">The shader entry-point function name (defaults to <c>"main"</c>).</param>
public readonly record struct ShaderDesc(ShaderStage Stage, ReadOnlyMemory<byte> Bytecode, string EntryPoint = "main");

/// <summary>Handle to a compiled GPU shader module.</summary>
public interface IShader : IDisposable
{
    /// <summary>The descriptor used to create this shader.</summary>
    ShaderDesc Description { get; }
}
