namespace Engine;

/// <summary>Swapchain providing image acquisition and resize support.</summary>
public interface ISwapchain : IDisposable
{
    Extent2D Extent { get; }
    uint ImageCount { get; }

    AcquireResult AcquireNextImage(out uint imageIndex);
    void Resize(Extent2D newExtent);
}

public enum AcquireResult
{
    Success,
    OutOfDate,
    Suboptimal,
    Error
}

