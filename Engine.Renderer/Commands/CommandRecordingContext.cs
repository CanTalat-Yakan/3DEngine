namespace Engine;

public sealed class CommandRecordingContext : IDisposable
{
    public IFrameContext FrameContext { get; }

    /// <summary>Frame-aware dynamic buffer allocator for transient GPU data (vertex, index, uniform).
    /// Allocations are valid only for the current frame.</summary>
    public DynamicBufferAllocator? DynamicAllocator { get; }

    internal CommandRecordingContext(IFrameContext frameContext, DynamicBufferAllocator? dynamicAllocator = null)
    {
        FrameContext = frameContext;
        DynamicAllocator = dynamicAllocator;
    }

    public void Dispose()
    {
        // FrameContext disposal handled by RendererContext after EndFrame; no-op here.
    }
}