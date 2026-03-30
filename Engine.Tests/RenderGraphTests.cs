using System;
using System.Collections.Generic;
using System.Linq;
using Engine;
using Xunit;

namespace Engine.Tests;

public class RenderGraphTests
{
    private sealed class TestNode : IRenderNode
    {
        public string Name { get; }
        public IReadOnlyCollection<string> Dependencies { get; }
        public TestNode(string name, params string[] deps)
        {
            Name = name;
            Dependencies = deps;
        }
        public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld) { }
    }

    [Fact]
    public void RenderGraph_Detects_Cycle()
    {
        var graph = new RenderGraph();
        graph.AddNode(new TestNode("A", "B"));
        graph.AddNode(new TestNode("B", "A"));
        Assert.Throws<InvalidOperationException>(() => graph.TopologicalOrder().ToList());
    }

    [Fact]
    public void RenderGraph_Produces_Topo_Order_For_Linear_Dependencies()
    {
        var graph = new RenderGraph();
        graph.AddNode(new TestNode("A"));
        graph.AddNode(new TestNode("B", "A"));
        graph.AddNode(new TestNode("C", "B"));

        var order = graph.TopologicalOrder().Select(n => n.Name).ToArray();
        Assert.Equal(new[] { "A", "B", "C" }, order);
    }
}
