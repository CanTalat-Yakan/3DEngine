namespace Engine;

public sealed class ClearNode : IRenderNode
{
    public string Name => "clear";
    public IReadOnlyCollection<string> Dependencies => Array.Empty<string>();

    public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        // Intentionally empty: the frame is already begun with a clear color.
        _ = ctx; _ = cmds; _ = renderWorld;
    }
}
