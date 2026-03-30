namespace Engine;

// Buffer abstractions

[Flags]
public enum BufferUsage
{
    None = 0,
    Vertex = 1 << 0,
    Index = 1 << 1,
    Uniform = 1 << 2,
    TransferSrc = 1 << 3,
    TransferDst = 1 << 4,
    Staging = 1 << 5
}

public enum CpuAccessMode
{
    None,
    Read,
    Write,
    ReadWrite
}

public readonly record struct BufferDesc(ulong Size, BufferUsage Usage, CpuAccessMode CpuAccess = CpuAccessMode.None);

public interface IBuffer : IDisposable
{
    BufferDesc Description { get; }
}

