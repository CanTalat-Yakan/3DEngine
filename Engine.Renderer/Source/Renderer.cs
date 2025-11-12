namespace Engine;

public sealed class Renderer : IDisposable
{
    private readonly List<IExtractSystem> _extractSystems = new();
    private readonly List<IPrepareSystem> _prepareSystems = new();
    private readonly List<IQueueSystem> _queueSystems = new();

    public RenderWorld RenderWorld { get; } = new();
    public RenderGraph Graph { get; } = new();
    public RendererContext Context { get; } = new();

    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        // Vulkan should be initialized by the backend plugin with an ISurfaceSource.

        // Register a default clear node
        Graph.AddNode(new ClearNode());

        _initialized = true;
    }

    public void AddExtractSystem(IExtractSystem sys) => _extractSystems.Add(sys);
    public void AddPrepareSystem(IPrepareSystem sys) => _prepareSystems.Add(sys);
    public void AddQueueSystem(IQueueSystem sys) => _queueSystems.Add(sys);
    public void AddNode(IRenderNode node) => Graph.AddNode(node);

    public void RenderFrame(object appWorld)
    {
        if (!_initialized) Initialize();

        foreach (var sys in _extractSystems)
            sys.Run(appWorld, RenderWorld);

        foreach (var sys in _prepareSystems)
            sys.Run(RenderWorld, Context);

        var ctx = Context.BeginFrame(RenderWorld, out var imageIndex);

        foreach (var sys in _queueSystems)
            sys.Run(RenderWorld, Context, ctx);

        foreach (var node in Graph.TopologicalOrder())
            node.Execute(Context, ctx, RenderWorld);

        Context.EndFrame(ctx, imageIndex);
    }

    public void Dispose()
    {
        Context.Dispose();
    }
}