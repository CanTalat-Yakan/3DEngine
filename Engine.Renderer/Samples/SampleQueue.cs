namespace Engine;

/// <summary>Sample queue system that binds the triangle pipeline, camera descriptor, and issues a draw call.</summary>
public sealed class SampleQueue : IQueueSystem
{
    private TrianglePipeline? _trianglePipeline;

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