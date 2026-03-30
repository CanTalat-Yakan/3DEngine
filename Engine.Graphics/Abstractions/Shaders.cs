namespace Engine;

// Shader abstractions

public enum ShaderStage
{
    Vertex,
    Fragment
}

public readonly record struct ShaderDesc(ShaderStage Stage, ReadOnlyMemory<byte> Bytecode, string EntryPoint = "main");

public interface IShader : IDisposable
{
    ShaderDesc Description { get; }
}

