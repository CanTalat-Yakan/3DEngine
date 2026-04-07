namespace Engine;

/// <summary>
/// A named node in the render graph with explicit dependencies.
/// Executed after all prepare and queue systems have run, in topological order.
/// </summary>
/// <seealso cref="RenderGraph"/>
/// <seealso cref="Renderer"/>
public interface IRenderNode
{
    /// <summary>Unique name identifying this node in the graph.</summary>
    string Name { get; }

    /// <summary>Names of other nodes that must execute before this one.</summary>
    IReadOnlyCollection<string> Dependencies { get; }

    /// <summary>Executes the render node, issuing GPU commands.</summary>
    /// <param name="ctx">The renderer context for GPU operations.</param>
    /// <param name="cmds">The command recording context for the current frame.</param>
    /// <param name="renderWorld">The render world containing render resources.</param>
    void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld);
}
