namespace Engine;

/// <summary>
/// Prepares GPU resources (buffers, descriptor sets, textures) from extracted render data.
/// Runs after <c>BeginFrame</c> (fence wait guarantees the in-flight slot is idle)
/// and before queue systems.
/// </summary>
public interface IPrepareSystem
{
    void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds);
}

