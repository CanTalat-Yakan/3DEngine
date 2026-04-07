namespace Engine;

/// <summary>
/// Records GPU commands from prepared render data.
/// Runs after prepare systems and before render graph execution.
/// </summary>
/// <seealso cref="Renderer"/>
/// <seealso cref="IPrepareSystem"/>
/// <seealso cref="IRenderNode"/>
public interface IQueueSystem
{
    /// <summary>Builds draw commands and render lists from prepared GPU data.</summary>
    /// <param name="renderWorld">The render world containing prepared data.</param>
    /// <param name="ctx">The renderer context for GPU operations.</param>
    /// <param name="cmds">The command recording context for the current frame.</param>
    void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds);
}
