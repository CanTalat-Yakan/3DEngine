namespace Engine;

/// <summary>
/// A named node in the render graph with explicit dependencies.
/// Executed after all prepare and queue systems have run.
/// </summary>
public interface IRenderNode
{
    string Name { get; }
    IReadOnlyCollection<string> Dependencies { get; }
    void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld);
}
