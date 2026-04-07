namespace Engine;

/// <summary>
/// Per-frame command recording context wrapping the GPU frame context and dynamic buffer allocator.
/// Passed through the prepare → queue → graph pipeline each frame.
/// </summary>
/// <seealso cref="Renderer"/>
/// <seealso cref="IFrameContext"/>
/// <seealso cref="DynamicBufferAllocator"/>
public sealed class CommandRecordingContext : IDisposable
{
    /// <summary>The underlying GPU frame context (command buffer, render pass, extent).</summary>
    public IFrameContext FrameContext { get; }

    /// <summary>Frame-aware dynamic buffer allocator for transient GPU data (vertex, index, uniform).
    /// Allocations are valid only for the current frame.</summary>
    public DynamicBufferAllocator? DynamicAllocator { get; }

    /// <summary>Creates a new command recording context for the given frame.</summary>
    /// <param name="frameContext">The GPU frame context.</param>
    /// <param name="dynamicAllocator">Optional dynamic buffer allocator.</param>
    internal CommandRecordingContext(IFrameContext frameContext, DynamicBufferAllocator? dynamicAllocator = null)
    {
        FrameContext = frameContext;
        DynamicAllocator = dynamicAllocator;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        // FrameContext disposal handled by RendererContext after EndFrame; no-op here.
    }
}