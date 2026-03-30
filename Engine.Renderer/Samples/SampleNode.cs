namespace Engine;

/// <summary>Sample render graph node that consumes camera descriptor data.</summary>
public sealed class SampleNode : IRenderNode
{
    public string Name => "sample";
    public IReadOnlyCollection<string> Dependencies => Array.Empty<string>();

    public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        _ = ctx;
        _ = cmds;

        var cameraSet = renderWorld.TryGet<IDescriptorSet>();
        _ = cameraSet;
    }
}
