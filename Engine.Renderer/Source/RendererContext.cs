namespace Engine;

// High-level facade over the low-level graphics backend (e.g., Vulkan).
public sealed class RendererContext : IDisposable
{
    private static readonly ILogger Logger = Log.For<RendererContext>();
    private readonly IGraphicsDevice _graphics;

    // Default constructor uses Vulkan backend; callers can inject a different IGraphicsDevice if desired.
    public RendererContext() : this(new GraphicsDevice()) { }

    public RendererContext(IGraphicsDevice graphics)
    {
        _graphics = graphics;
    }

    public bool IsInitialized => _graphics.IsInitialized;

    public GraphicsAdapterInfo AdapterInfo => _graphics.AdapterInfo;

    public void Initialize(ISurfaceSource surfaceSource, string appName = "3DEngine")
    {
        if (IsInitialized) return;
        _graphics.Initialize(surfaceSource, appName);
    }

    public CommandRecordingContext BeginFrame(RenderWorld world, out uint imageIndex)
    {
        var clear = world.TryGet<RenderClearColor>() is { } cc
            ? new ClearColor(cc.R, cc.G, cc.B, cc.A)
            : ClearColor.Black;

        var frame = _graphics.BeginFrame(clear);
        imageIndex = frame.FrameIndex; // treat frame index as swapchain image index
        return new CommandRecordingContext(frame);
    }

    public CommandRecordingContext BeginFrame(RenderWorld world) => BeginFrame(world, out _);

    public void EndFrame(CommandRecordingContext ctx, uint imageIndex)
    {
        _graphics.EndFrame(ctx.FrameContext);
        ctx.Dispose();
    }

    public void EndFrame(CommandRecordingContext ctx) => EndFrame(ctx, ctx.FrameContext.FrameIndex);

    public void OnResize() => _graphics.OnResize();

    public void Dispose() => _graphics.Dispose();
}