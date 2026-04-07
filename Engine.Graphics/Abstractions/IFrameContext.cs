namespace Engine;

/// <summary>
/// Per-frame rendering context providing access to the command buffer, render pass, framebuffer,
/// and in-flight frame indices for GPU resource double/triple buffering.
/// </summary>
/// <seealso cref="IGraphicsDevice"/>
public interface IFrameContext : IDisposable
{
    /// <summary>Absolute frame index (monotonically increasing).</summary>
    uint FrameIndex { get; }

    /// <summary>The command buffer for recording draw commands this frame.</summary>
    ICommandBuffer CommandBuffer { get; }

    /// <summary>The active render pass for this frame.</summary>
    IRenderPass RenderPass { get; }

    /// <summary>The framebuffer (attachments) for this frame's render pass.</summary>
    IFramebuffer Framebuffer { get; }

    /// <summary>Current swapchain extent (width × height) in pixels.</summary>
    Extent2D Extent { get; }

    /// <summary>
    /// Index of the current in-flight frame slot (0 .. <see cref="FramesInFlight"/>-1).
    /// Use this to index per-frame GPU resources (e.g. vertex/index buffers) so that
    /// writes on the CPU never race with reads still in flight on the GPU.
    /// </summary>
    int InFlightIndex { get; }

    /// <summary>Total number of frames that may be in flight simultaneously.</summary>
    int FramesInFlight { get; }
}
