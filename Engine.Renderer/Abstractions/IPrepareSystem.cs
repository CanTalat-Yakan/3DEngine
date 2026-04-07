namespace Engine;

/// <summary>
/// Prepares GPU resources (buffers, descriptor sets, textures) from extracted render data.
/// Runs after <c>BeginFrame</c> (fence wait guarantees the in-flight slot is idle)
/// and before queue systems.
/// </summary>
/// <seealso cref="Renderer"/>
/// <seealso cref="IExtractSystem"/>
/// <seealso cref="IQueueSystem"/>
public interface IPrepareSystem
{
    /// <summary>Uploads or updates GPU resources using data from the render world.</summary>
    /// <param name="renderWorld">The render world containing extracted data.</param>
    /// <param name="ctx">The renderer context for GPU resource creation.</param>
    /// <param name="cmds">The command recording context for the current frame.</param>
    void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds);
}

