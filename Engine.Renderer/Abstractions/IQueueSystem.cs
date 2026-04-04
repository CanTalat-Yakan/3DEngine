namespace Engine;

/// <summary>
/// Records GPU commands from prepared render data.
/// Runs after prepare systems and before render graph execution.
/// </summary>
public interface IQueueSystem
{
    void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds);
}
