namespace Engine;

public sealed class QueuePlaceholder : IQueueSystem
{
    public void Run(Engine.RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds)
    {
        // TODO: Record pipeline state & draw calls referencing prepared GPU resources.
    }
}