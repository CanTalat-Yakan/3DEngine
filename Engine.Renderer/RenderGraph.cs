namespace Engine;

/// <summary>Directed acyclic graph of render nodes with dependency-based topological execution order.</summary>
/// <remarks>
/// <para>
/// Nodes are added via <see cref="AddNode"/> and their dependencies (declared by
/// <see cref="IRenderNode.Dependencies"/>) are tracked as directed edges.
/// <see cref="TopologicalOrder"/> returns nodes in a valid execution order using
/// Kahn's algorithm, ensuring each node runs only after all its dependencies have completed.
/// </para>
/// <para>
/// Dependencies referencing nodes not present in the graph are silently ignored, allowing
/// optional dependencies that may or may not be registered.
/// </para>
/// </remarks>
/// <seealso cref="IRenderNode"/>
public sealed class RenderGraph : IDisposable
{
    private readonly Dictionary<string, IRenderNode> _nodes = new();
    private readonly Dictionary<string, HashSet<string>> _edges = new();

    /// <summary>Adds a render node to the graph.</summary>
    /// <param name="node">The render node to register. Must have a unique <see cref="IRenderNode.Name"/>.</param>
    /// <exception cref="InvalidOperationException">A node with the same name is already registered.</exception>
    public void AddNode(IRenderNode node)
    {
        if (_nodes.ContainsKey(node.Name))
            throw new InvalidOperationException($"Node '{node.Name}' already exists.");
        _nodes[node.Name] = node;
        _edges[node.Name] = new HashSet<string>(node.Dependencies);
    }

    /// <summary>Returns nodes in topological order using Kahn's algorithm. Throws on cycles.</summary>
    /// <returns>An enumerable of <see cref="IRenderNode"/> instances in dependency-first execution order.</returns>
    /// <exception cref="InvalidOperationException">The graph contains a cycle.</exception>
    /// <remarks>
    /// <para>
    /// Only edges pointing to nodes that actually exist in the graph are considered;
    /// references to unregistered nodes are treated as optional and silently ignored.
    /// </para>
    /// <para>
    /// The algorithm computes in-degree for each node, starts with zero-dependency nodes,
    /// and iteratively removes satisfied edges until all nodes are ordered.
    /// </para>
    /// </remarks>
    public IEnumerable<IRenderNode> TopologicalOrder()
    {
        // Only keep edges to nodes that actually exist in the graph (ignore optional/unregistered deps)
        var registered = _nodes.Keys.ToHashSet();
        var inEdges = _edges.ToDictionary(
            kv => kv.Key,
            kv => kv.Value.Where(dep => registered.Contains(dep)).ToHashSet());

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

    /// <summary>Disposes all render nodes that implement <see cref="IDisposable"/>.</summary>
    public void Dispose()
    {
        foreach (var node in _nodes.Values)
        {
            if (node is IDisposable disposable)
                disposable.Dispose();
        }
        _nodes.Clear();
        _edges.Clear();
    }
}