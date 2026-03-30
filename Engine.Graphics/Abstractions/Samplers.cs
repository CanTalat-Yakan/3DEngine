namespace Engine;

// Sampler abstractions

public enum SamplerFilter
{
    Nearest,
    Linear
}

public enum SamplerAddressMode
{
    ClampToEdge,
    Repeat,
    MirrorRepeat
}

public readonly record struct SamplerDesc(
    SamplerFilter MinFilter,
    SamplerFilter MagFilter,
    SamplerAddressMode AddressU,
    SamplerAddressMode AddressV,
    SamplerAddressMode AddressW);

public interface ISampler : IDisposable
{
    SamplerDesc Description { get; }
}

