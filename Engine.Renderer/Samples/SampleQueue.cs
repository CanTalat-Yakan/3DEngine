namespace Engine;

/// <summary>Sample queue system that binds the triangle pipeline, camera descriptor, and issues a draw call.</summary>
/// <remarks>
/// Lazily creates a <see cref="TrianglePipeline"/> on first run.  Each frame it binds the
/// pipeline and camera descriptor set, then draws a fullscreen triangle (3 vertices generated
/// in the vertex shader).
/// </remarks>
/// <seealso cref="SampleExtract"/>
/// <seealso cref="SamplePrepare"/>
/// <seealso cref="TrianglePipeline"/>
public sealed class SampleQueue : IQueueSystem
{
    private TrianglePipeline? _trianglePipeline;

    /// <inheritdoc />
    /// <param name="renderWorld">The render world containing the camera <see cref="IDescriptorSet"/>.</param>
    /// <param name="ctx">Renderer context providing GPU device access.</param>
    /// <param name="cmds">Command recording context for the current frame.</param>
    public void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds)
    {
        var cameraSet = renderWorld.TryGet<IDescriptorSet>();
        if (cameraSet is null)
            return;

        if (_trianglePipeline is null)
        {
            var frameContext = cmds.FrameContext;

            TriangleShaders.EnsureLoaded();
            ReadOnlyMemory<byte> vertexSpirv = TriangleShaders.Vertex;
            ReadOnlyMemory<byte> fragmentSpirv = TriangleShaders.Fragment;

            _trianglePipeline = new TrianglePipeline(ctx.Graphics, frameContext.RenderPass, vertexSpirv, fragmentSpirv);
        }

        var pipeline = _trianglePipeline!.Pipeline;
        var cmd = cmds.FrameContext.CommandBuffer;

        ctx.Graphics.BindGraphicsPipeline(cmd, pipeline);
        ctx.Graphics.BindDescriptorSet(cmd, pipeline, cameraSet);
        ctx.Graphics.Draw(cmd, vertexCount: 3);
    }
}