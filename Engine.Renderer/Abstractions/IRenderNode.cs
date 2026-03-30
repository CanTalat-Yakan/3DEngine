namespace Engine;

/// <summary>A named node in the render graph with explicit dependencies.</summary>
public interface IRenderNode
{
    string Name { get; }
    IReadOnlyCollection<string> Dependencies { get; }
    void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld);
}

