using System.Diagnostics;

namespace Engine;

/// <summary>
/// Orchestrates the rendering pipeline: extract → prepare → queue → graph execution.
/// </summary>
/// <remarks>
/// <para>
/// Each frame follows a fixed pipeline:
/// <list>
///   <item><description><b>Extract</b> copies relevant game-world data into the <see cref="RenderWorld"/>.</description></item>
///   <item><description><b>Prepare</b> uploads GPU resources (buffers, textures) using the extracted data.</description></item>
///   <item><description><b>Queue</b> builds draw commands / render lists.</description></item>
///   <item><description><b>Graph</b> executes <see cref="IRenderNode"/> instances in topological order.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="RenderWorld"/>
/// <seealso cref="RenderGraph"/>
/// <seealso cref="RendererContext"/>
public sealed class Renderer : IDisposable
{
    private static readonly ILogger Logger = Log.For<Renderer>();

    private readonly List<IExtractSystem> _extractSystems = new();
    private readonly List<IPrepareSystem> _prepareSystems = new();
    private readonly List<IQueueSystem> _queueSystems = new();
    
    /// <summary>The render-thread resource container.</summary>
    public RenderWorld RenderWorld { get; } = new();

    /// <summary>The render graph defining the execution order of render passes.</summary>
    public RenderGraph Graph { get; } = new();

    /// <summary>The high-level graphics context (device, camera resources, dynamic allocator).</summary>
    public RendererContext Context { get; }

    /// <summary>Frame timing and adapter diagnostics.</summary>
    public RendererDiagnostics Diagnostics { get; } = new();

    private bool _initialized;
    private Extent2D _cachedSurfaceExtent;

    /// <summary>Creates a new <see cref="Renderer"/> with the specified graphics context.</summary>
    /// <param name="context">The renderer context wrapping the graphics device.</param>
    public Renderer(RendererContext context) =>
        Context = context;

    /// <summary>Initializes the renderer, setting up diagnostics and the default render graph nodes.</summary>
    public void Initialize()
    {
        if (_initialized) return;
        Logger.Info("Initializing Renderer - setting up diagnostics and render graph...");
        var sw = Stopwatch.StartNew();

        Diagnostics.Initialize(Context.AdapterInfo);
        Logger.Debug("Renderer diagnostics initialized.");

        Graph.AddNode(new SampleNode());
        Logger.Debug("Default SampleNode added to render graph.");

        _initialized = true;
        Logger.Info($"Renderer initialized in {sw.ElapsedMilliseconds}ms.");
    }

    /// <summary>Registers an extract system that copies game-world data into the <see cref="RenderWorld"/> each frame.</summary>
    /// <param name="sys">The extract system to add.</param>
    public void AddExtractSystem(IExtractSystem sys) => _extractSystems.Add(sys);

    /// <summary>Registers a prepare system that uploads GPU resources each frame.</summary>
    /// <param name="sys">The prepare system to add.</param>
    public void AddPrepareSystem(IPrepareSystem sys) => _prepareSystems.Add(sys);

    /// <summary>Registers a queue system that builds draw commands each frame.</summary>
    /// <param name="sys">The queue system to add.</param>
    public void AddQueueSystem(IQueueSystem sys) => _queueSystems.Add(sys);

    /// <summary>Adds a render graph node that will execute during the graph phase.</summary>
    /// <param name="node">The render node to add.</param>
    public void AddNode(IRenderNode node) => Graph.AddNode(node);

    /// <summary>Executes one full render frame: extract → prepare → queue → graph.</summary>
    /// <param name="world">The game world to read data from during the extract phase.</param>
    public void RenderFrame(World world)
    {
        if (!_initialized) Initialize();

        Logger.FrameTrace("RenderFrame: Running extract systems...");
        foreach (var sys in _extractSystems)
            sys.Run(world, RenderWorld);

        Logger.FrameTrace("RenderFrame: Beginning frame...");
        var ctx = Context.BeginFrame(RenderWorld, out var imageIndex);
        SyncSurfaceInfo(ctx.FrameContext.Extent);
        UpdateDiagnostics(ctx.FrameContext.Extent);

        Logger.FrameTrace("RenderFrame: Running prepare systems...");
        foreach (var sys in _prepareSystems)
            sys.Run(RenderWorld, Context, ctx);

        Logger.FrameTrace("RenderFrame: Running queue systems...");
        foreach (var sys in _queueSystems)
            sys.Run(RenderWorld, Context, ctx);

        Logger.FrameTrace("RenderFrame: Executing render graph nodes...");
        foreach (var node in Graph.TopologicalOrder())
            node.Execute(Context, ctx, RenderWorld);

        Logger.FrameTrace("RenderFrame: Ending frame...");
        Context.EndFrame(ctx, imageIndex);
    }

    /// <summary>Syncs the render surface dimensions from the current swapchain extent.</summary>
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

    /// <summary>Records frame diagnostics (adapter info, extent, surface revision).</summary>
    private void UpdateDiagnostics(Extent2D extent)
    {
        var surface = RenderWorld.TryGet<RenderSurfaceInfo>();
        var effectiveExtent = _cachedSurfaceExtent.Width == 0 ? extent : _cachedSurfaceExtent;
        Diagnostics.RecordFrame(Context.AdapterInfo, effectiveExtent, surface?.Revision ?? 0);
    }

    /// <summary>Clamps an unsigned pixel dimension to a positive integer, minimum 1.</summary>
    private static int ToIntDimension(uint value)
    {
        if (value == 0) return 1;
        if (value > int.MaxValue) return int.MaxValue;
        return (int)value;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Logger.Info("Disposing Renderer and underlying graphics context...");

        DisposeSystems(_queueSystems);
        DisposeSystems(_prepareSystems);
        DisposeSystems(_extractSystems);
        Logger.Debug("Render systems disposed.");

        Graph.Dispose();
        Logger.Debug("Render graph nodes disposed.");

        Context.Dispose();
        Logger.Info("Renderer disposed.");
    }

    /// <summary>Disposes all <see cref="IDisposable"/> systems in a list and clears it.</summary>
    private static void DisposeSystems<T>(List<T> systems)
    {
        foreach (var sys in systems)
            if (sys is IDisposable disposable)
                disposable.Dispose();

        systems.Clear();
    }
}