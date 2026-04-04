namespace Engine;

/// <summary>Per-frame rendering context with command buffer and render pass.</summary>
public interface IFrameContext : IDisposable
{
    uint FrameIndex { get; }
    ICommandBuffer CommandBuffer { get; }
    IRenderPass RenderPass { get; }
    IFramebuffer Framebuffer { get; }
    Extent2D Extent { get; }

    /// <summary>Index of the current in-flight frame slot (0 .. <see cref="FramesInFlight"/>-1).
    /// Use this to index per-frame GPU resources (e.g. vertex/index buffers) so that
    /// writes on the CPU never race with reads still in flight on the GPU.</summary>
    int InFlightIndex { get; }

    /// <summary>Total number of frames that may be in flight simultaneously.</summary>
    int FramesInFlight { get; }
}

