namespace Engine;

public interface IRenderNode
{
    string Name { get; }
    IReadOnlyCollection<string> Dependencies { get; }
    void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld);
}