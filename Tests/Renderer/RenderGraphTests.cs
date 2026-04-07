using FluentAssertions;
using Xunit;

namespace Engine.Tests.Renderer;

[Trait("Category", "Unit")]
public class RenderGraphTests
{
    // ── Helpers ─────────────────────────────────────────────────────────

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

    private sealed class DisposableTestNode : IRenderNode, IDisposable
    {
        public string Name { get; }
        public IReadOnlyCollection<string> Dependencies { get; } = Array.Empty<string>();
        public bool IsDisposed;
        public DisposableTestNode(string name) => Name = name;
        public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld) { }
        public void Dispose() => IsDisposed = true;
    }

    // ── Topological order ──────────────────────────────────────────────

    [Fact]
    public void TopologicalOrder_Linear_Dependencies()
    {
        using var graph = new RenderGraph();
        graph.AddNode(new TestNode("A"));
        graph.AddNode(new TestNode("B", "A"));
        graph.AddNode(new TestNode("C", "B"));

        var order = graph.TopologicalOrder().Select(n => n.Name).ToArray();

        order.Should().Equal("A", "B", "C");
    }

    [Fact]
    public void TopologicalOrder_Diamond_Dependencies_Are_Resolved()
    {
        using var graph = new RenderGraph();
        //     A
        //    / \
        //   B   C
        //    \ /
        //     D
        graph.AddNode(new TestNode("A"));
        graph.AddNode(new TestNode("B", "A"));
        graph.AddNode(new TestNode("C", "A"));
        graph.AddNode(new TestNode("D", "B", "C"));

        var order = graph.TopologicalOrder().Select(n => n.Name).ToList();

        order.First().Should().Be("A");
        order.Last().Should().Be("D");
        order.IndexOf("B").Should().BeLessThan(order.IndexOf("D"));
        order.IndexOf("C").Should().BeLessThan(order.IndexOf("D"));
    }

    [Fact]
    public void TopologicalOrder_Single_Node_Returns_That_Node()
    {
        using var graph = new RenderGraph();
        graph.AddNode(new TestNode("OnlyNode"));

        var order = graph.TopologicalOrder().Select(n => n.Name).ToArray();

        order.Should().Equal("OnlyNode");
    }

    [Fact]
    public void TopologicalOrder_Independent_Nodes_All_Appear()
    {
        using var graph = new RenderGraph();
        graph.AddNode(new TestNode("X"));
        graph.AddNode(new TestNode("Y"));
        graph.AddNode(new TestNode("Z"));

        var order = graph.TopologicalOrder().Select(n => n.Name).ToArray();

        order.Should().HaveCount(3);
        order.Should().Contain("X");
        order.Should().Contain("Y");
        order.Should().Contain("Z");
    }

    // ── Cycle detection ────────────────────────────────────────────────

    [Fact]
    public void TopologicalOrder_Detects_Cycle()
    {
        var graph = new RenderGraph();
        graph.AddNode(new TestNode("A", "B"));
        graph.AddNode(new TestNode("B", "A"));

        var act = () => graph.TopologicalOrder().ToList();

        act.Should().Throw<InvalidOperationException>();
        graph.Dispose();
    }

    // ── Optional / missing dependencies ────────────────────────────────

    [Fact]
    public void Optional_Missing_Dependency_Is_Ignored()
    {
        using var graph = new RenderGraph();
        graph.AddNode(new TestNode("A"));
        graph.AddNode(new TestNode("B", "A", "NonExistent"));

        var order = graph.TopologicalOrder().Select(n => n.Name).ToArray();

        order.Should().Equal("A", "B");
    }

    // ── Duplicate names ────────────────────────────────────────────────

    [Fact]
    public void AddNode_Duplicate_Name_Throws()
    {
        var graph = new RenderGraph();
        graph.AddNode(new TestNode("A"));

        var act = () => graph.AddNode(new TestNode("A"));

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*'A'*");
        graph.Dispose();
    }

    // ── Dispose ────────────────────────────────────────────────────────

    [Fact]
    public void Dispose_Disposes_Disposable_Nodes()
    {
        var node = new DisposableTestNode("A");
        var graph = new RenderGraph();
        graph.AddNode(node);

        graph.Dispose();

        node.IsDisposed.Should().BeTrue();
    }
}



