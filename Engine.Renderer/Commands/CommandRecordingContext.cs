namespace Engine;

public sealed class CommandRecordingContext : IDisposable
{
    public IFrameContext FrameContext { get; }

    internal CommandRecordingContext(IFrameContext frameContext)
    {
        FrameContext = frameContext;
    }

    public void Dispose()
    {
        // FrameContext disposal handled by RendererContext after EndFrame; no-op here.
    }
}