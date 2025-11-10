using System.Collections.Concurrent;
using Vortice.Vulkan;

namespace Engine;

// High-level frame stages similar to Bevy's Extract/Prepare/Queue/Render
public enum RenderStage
{
    Extract,
    Prepare,
    Queue,
    Execute
}

// Simple resource world for GPU-facing data (mirrors App World pattern)
public sealed class RenderWorld
{
    private readonly ConcurrentDictionary<Type, object> _resources = new();
    public bool Contains<T>() where T : notnull => _resources.ContainsKey(typeof(T));
    public T Get<T>() where T : notnull => (T)_resources[typeof(T)];
    public T? TryGet<T>() where T : notnull => _resources.TryGetValue(typeof(T), out var obj) ? (T?)obj : default;
    public void Set<T>(T value) where T : notnull => _resources[typeof(T)] = value!;
    public bool Remove<T>() where T : notnull => _resources.TryRemove(typeof(T), out _);
}

// GPU-facing clear color extracted from app world
public readonly struct RenderClearColor
{
    public readonly float R, G, B, A;
    public RenderClearColor(float r, float g, float b, float a) { R = r; G = g; B = b; A = a; }
}

// Systems hooking into stages
public interface IExtractSystem { void Run(object appWorld, RenderWorld renderWorld); }
public interface IPrepareSystem { void Run(RenderWorld renderWorld, VulkanContext vk); }
public interface IQueueSystem { void Run(RenderWorld renderWorld, VulkanContext vk, CommandRecordingContext cmds); }

// Basic render graph node contract
public interface IRenderNode
{
    string Name { get; }
    IReadOnlyCollection<string> Dependencies { get; }
    void Execute(VulkanContext vk, CommandRecordingContext cmds, RenderWorld renderWorld);
}

// Command recording context abstraction (could be expanded later for multi-pass)
public sealed class CommandRecordingContext
{
    // Placeholder for future VkCommandBuffer (not used yet in stub implementation)
}

// Minimal Vulkan setup holder (stubbed for now)
public sealed class VulkanContext : IDisposable
{
    public bool IsInitialized { get; private set; }

    public void Initialize(string appName = "3DEngine")
    {
        // Stub: real vkCreateInstance / device selection will be added later.
        IsInitialized = true;
    }

    public void Dispose()
    {
        // Stub: destroy device / instance when implemented.
        IsInitialized = false;
    }
}

// Simple render graph maintaining dependency ordering
public sealed class RenderGraph
{
    private readonly Dictionary<string, IRenderNode> _nodes = new();
    private readonly Dictionary<string, HashSet<string>> _edges = new();

    public void AddNode(IRenderNode node)
    {
        if (_nodes.ContainsKey(node.Name))
            throw new InvalidOperationException($"Node '{node.Name}' already exists.");
        _nodes[node.Name] = node;
        _edges[node.Name] = new HashSet<string>(node.Dependencies);
    }

    public IEnumerable<IRenderNode> TopologicalOrder()
    {
        // Kahn's algorithm (simplified)
        var inEdges = _edges.ToDictionary(kv => kv.Key, kv => kv.Value.ToHashSet());
        var noDep = new Queue<string>(inEdges.Where(kv => kv.Value.Count == 0).Select(kv => kv.Key));
        var result = new List<IRenderNode>();
        while (noDep.Count > 0)
        {
            var n = noDep.Dequeue();
            result.Add(_nodes[n]);
            foreach (var kv in inEdges)
            {
                if (kv.Value.Remove(n) && kv.Value.Count == 0)
                    if (!result.Any(r => r.Name == kv.Key) && !noDep.Contains(kv.Key))
                        noDep.Enqueue(kv.Key);
            }
        }
        if (result.Count != _nodes.Count)
            throw new InvalidOperationException("Cycle detected in render graph.");
        return result;
    }
}

// Example node: placeholder clear
public sealed class ClearNode : IRenderNode
{
    public string Name => "clear";
    public IReadOnlyCollection<string> Dependencies => Array.Empty<string>();

    public void Execute(VulkanContext vk, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        // Read clear color if available (extracted earlier)
        if (renderWorld.TryGet<RenderClearColor>() is { } cc)
        {
            _ = cc; // Placeholder until Vulkan hookup; prevents warnings
        }
        // Stub: will record a render pass clear when Vulkan hookup is implemented.
    }
}

public sealed class Renderer : IDisposable
{
    private readonly List<IExtractSystem> _extractSystems = new();
    private readonly List<IPrepareSystem> _prepareSystems = new();
    private readonly List<IQueueSystem> _queueSystems = new();

    public RenderWorld RenderWorld { get; } = new();
    public RenderGraph Graph { get; } = new();
    public VulkanContext Vulkan { get; } = new();

    private bool _initialized;

    public void Initialize()
    {
        if (_initialized) return;
        Vulkan.Initialize();

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

        // 1. Extract
        foreach (var sys in _extractSystems)
            sys.Run(appWorld, RenderWorld);

        // 2. Prepare
        foreach (var sys in _prepareSystems)
            sys.Run(RenderWorld, Vulkan);

        // 3. Queue (record command buffer) — stubbed
        var recordingCtx = new CommandRecordingContext();
        foreach (var sys in _queueSystems)
            sys.Run(RenderWorld, Vulkan, recordingCtx);

        // 4. Execute render graph nodes (linear order respecting dependencies)
        foreach (var node in Graph.TopologicalOrder())
            node.Execute(Vulkan, recordingCtx, RenderWorld);

        // Stub: submit GPU work when Vulkan is wired
    }

    public void Dispose()
    {
        Vulkan.Dispose();
    }
}