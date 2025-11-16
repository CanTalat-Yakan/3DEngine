namespace Engine;

public sealed class SampleQueue : IQueueSystem
{
    public void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds)
    {
        // TODO: Record pipeline state & draw calls referencing prepared GPU resources.
    }
}