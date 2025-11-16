namespace Engine;

public sealed class SampleNode : IRenderNode
{
    public string Name => "sample";
    public IReadOnlyCollection<string> Dependencies => Array.Empty<string>();

    public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        // Forward-pass placeholder: ensure the camera descriptor set has been prepared
        // by SamplePrepare so that future pipeline binding/draw logic can consume it here.
        _ = ctx;
        _ = cmds;

        var cameraSet = renderWorld.TryGet<IDescriptorSet>();
        _ = cameraSet;
    }
}
