namespace Engine;

/// <summary>Records GPU commands from prepared render data.</summary>
public interface IQueueSystem
{
    void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds);
}

