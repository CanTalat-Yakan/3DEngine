namespace Engine;

public sealed class SampleNode : IRenderNode
{
    public string Name => "sample";
    public IReadOnlyCollection<string> Dependencies => Array.Empty<string>();

    public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        _ = ctx; _ = cmds; _ = renderWorld;
    }
}
