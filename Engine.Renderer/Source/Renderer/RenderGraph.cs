namespace Engine;

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
        // Dependencies may refer to nodes that are added later; we only enforce cycle detection
        // during topological ordering.
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