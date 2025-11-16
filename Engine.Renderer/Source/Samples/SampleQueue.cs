namespace Engine;

public sealed class SampleQueue : IQueueSystem
{
    private TrianglePipeline? _trianglePipeline;

    public void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds)
    {
        // Retrieve the camera descriptor set prepared in SamplePrepare.
        var cameraSet = renderWorld.TryGet<IDescriptorSet>();
        if (cameraSet is null)
            return;

        // Lazily create the triangle pipeline using the current frame's render pass.
        if (_trianglePipeline is null)
        {
            var frameContext = cmds.FrameContext;

            // Ensure SPIR-V is available (compile GLSL if necessary) and then create the pipeline.
            TriangleShaders.EnsureLoaded();
            ReadOnlyMemory<byte> vertexSpirv = TriangleShaders.Vertex;
            ReadOnlyMemory<byte> fragmentSpirv = TriangleShaders.Fragment;

            _trianglePipeline = new TrianglePipeline(ctx.Graphics, frameContext.RenderPass, vertexSpirv, fragmentSpirv);
        }

        var pipeline = _trianglePipeline!.Pipeline;
        var cmd = cmds.FrameContext.CommandBuffer;

        // Bind pipeline and camera descriptor, then draw a single triangle.
        ctx.Graphics.BindGraphicsPipeline(cmd, pipeline);
        ctx.Graphics.BindDescriptorSet(cmd, pipeline, cameraSet);
        ctx.Graphics.Draw(cmd, vertexCount: 3);
    }
}