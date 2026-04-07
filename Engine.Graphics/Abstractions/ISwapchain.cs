namespace Engine;

/// <summary>Swapchain abstraction providing image acquisition, extent queries, and resize support.</summary>
/// <seealso cref="IGraphicsDevice"/>
public interface ISwapchain : IDisposable
{
    /// <summary>Current swapchain extent (width × height) in pixels.</summary>
    Extent2D Extent { get; }

    /// <summary>Number of images in the swapchain.</summary>
    uint ImageCount { get; }

    /// <summary>Acquires the next available swapchain image for rendering.</summary>
    /// <param name="imageIndex">When returning <see cref="AcquireResult.Success"/>, the index of the acquired image.</param>
    /// <returns>The result of the acquisition attempt.</returns>
    AcquireResult AcquireNextImage(out uint imageIndex);

    /// <summary>Resizes the swapchain to the specified extent.</summary>
    /// <param name="newExtent">The new target extent in pixels.</param>
    void Resize(Extent2D newExtent);
}

/// <summary>Result of a swapchain image acquisition attempt.</summary>
public enum AcquireResult
{
    /// <summary>Image acquired successfully.</summary>
    Success,
    /// <summary>Swapchain is out of date and must be recreated (e.g., window resize).</summary>
    OutOfDate,
    /// <summary>Image acquired but swapchain is suboptimal - recreation recommended.</summary>
    Suboptimal,
    /// <summary>Acquisition failed due to an unrecoverable error.</summary>
    Error
}
