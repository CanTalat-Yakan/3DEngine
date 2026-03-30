using System.Diagnostics;

namespace Engine;

public sealed class Renderer : IDisposable
{
    private static readonly ILogger Logger = Log.For<Renderer>();

    private readonly List<IExtractSystem> _extractSystems = new();
    private readonly List<IPrepareSystem> _prepareSystems = new();
    private readonly List<IQueueSystem> _queueSystems = new();

    public RenderWorld RenderWorld { get; } = new();
    public RenderGraph Graph { get; } = new();
    public RendererContext Context { get; } = new();
    public RendererDiagnostics Diagnostics { get; } = new();

    private bool _initialized;
    private Extent2D _cachedSurfaceExtent;

    public Renderer() { }
    public Renderer(RendererContext context)
    {
        Context = context;
    }

    public void Initialize()
    {
        if (_initialized) return;
        Logger.Info("Initializing Renderer — setting up diagnostics and render graph...");
        var sw = Stopwatch.StartNew();

        Diagnostics.Initialize(Context.AdapterInfo);
        Logger.Debug("Renderer diagnostics initialized.");

        Graph.AddNode(new SampleNode());
        Logger.Debug("Default SampleNode added to render graph.");

        _initialized = true;
        Logger.Info($"Renderer initialized in {sw.ElapsedMilliseconds}ms.");
    }

    public void AddExtractSystem(IExtractSystem sys) => _extractSystems.Add(sys);
    public void AddPrepareSystem(IPrepareSystem sys) => _prepareSystems.Add(sys);
    public void AddQueueSystem(IQueueSystem sys) => _queueSystems.Add(sys);
    public void AddNode(IRenderNode node) => Graph.AddNode(node);

    public void RenderFrame(object appWorld)
    {
        if (!_initialized) Initialize();

        Logger.FrameTrace("RenderFrame: Running extract systems...");
        foreach (var sys in _extractSystems)
            sys.Run(appWorld, RenderWorld);

        Logger.FrameTrace("RenderFrame: Running prepare systems...");
        foreach (var sys in _prepareSystems)
            sys.Run(RenderWorld, Context);

        Logger.FrameTrace("RenderFrame: Beginning frame...");
        var ctx = Context.BeginFrame(RenderWorld, out var imageIndex);
        SyncSurfaceInfo(ctx.FrameContext.Extent);
        UpdateDiagnostics(ctx.FrameContext.Extent);

        Logger.FrameTrace("RenderFrame: Running queue systems...");
        foreach (var sys in _queueSystems)
            sys.Run(RenderWorld, Context, ctx);

        Logger.FrameTrace("RenderFrame: Executing render graph nodes...");
        foreach (var node in Graph.TopologicalOrder())
            node.Execute(Context, ctx, RenderWorld);

        Logger.FrameTrace("RenderFrame: Ending frame...");
        Context.EndFrame(ctx, imageIndex);
    }

    private void SyncSurfaceInfo(Extent2D extent)
    {
        var surface = RenderWorld.TryGet<RenderSurfaceInfo>();
        if (surface is null)
        {
            surface = new RenderSurfaceInfo();
            RenderWorld.Set(surface);
        }

        if (surface.Apply(ToIntDimension(extent.Width), ToIntDimension(extent.Height)))
        {
            _cachedSurfaceExtent = new Extent2D((uint)surface.Width, (uint)surface.Height);
            Log.For<Renderer>().Info($"Surface resized to {surface.Width}x{surface.Height}");
        }
    }

    private void UpdateDiagnostics(Extent2D extent)
    {
        var surface = RenderWorld.TryGet<RenderSurfaceInfo>();
        var effectiveExtent = _cachedSurfaceExtent.Width == 0 ? extent : _cachedSurfaceExtent;
        Diagnostics.RecordFrame(Context.AdapterInfo, effectiveExtent, surface?.Revision ?? 0);
    }

    private static int ToIntDimension(uint value)
    {
        if (value == 0) return 1;
        if (value > int.MaxValue) return int.MaxValue;
        return (int)value;
    }

    public void Dispose()
    {
        Logger.Info("Disposing Renderer and underlying graphics context...");
        Context.Dispose();
        Logger.Info("Renderer disposed.");
    }
}