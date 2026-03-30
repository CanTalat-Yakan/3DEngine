namespace Engine;

// Image abstractions

public enum ImageFormat
{
    Undefined,
    R8G8B8A8_UNorm,
    B8G8R8A8_UNorm,
    D24_UNorm_S8_UInt,
    D32_Float
}

[Flags]
public enum ImageUsage
{
    None          = 0,
    ColorAttachment      = 1 << 0,
    DepthStencilAttachment = 1 << 1,
    Sampled       = 1 << 2,
    TransferSrc   = 1 << 3,
    TransferDst   = 1 << 4
}

public readonly record struct ImageDesc(Extent2D Extent, ImageFormat Format, ImageUsage Usage);

public interface IImage : IDisposable
{
    ImageDesc Description { get; }
}

public interface IImageView : IDisposable
{
    IImage Image { get; }
}

