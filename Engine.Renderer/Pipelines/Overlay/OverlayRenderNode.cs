namespace Engine;

/// <summary>
/// Render graph node that draws a full-screen textured quad overlay (e.g., web UI).
/// Expects an <see cref="OverlayTexture"/> resource in the <see cref="RenderWorld"/>.
/// Runs after the "sample" node so the overlay composites on top of the 3D scene.
/// </summary>
public sealed class OverlayRenderNode : IRenderNode
{
    private static readonly ILogger Logger = Log.Category("Engine.Renderer.Overlay");

    public string Name => "overlay";
    public IReadOnlyCollection<string> Dependencies { get; } = new[] { "sample" };

    private OverlayPipeline? _pipeline;

    public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        var overlay = renderWorld.TryGet<OverlayTexture>();
        if (overlay is null || overlay.DescriptorSet is null)
            return;

        if (_pipeline is null)
        {
            Logger.Info("Creating overlay graphics pipeline (alpha-blended full-screen quad)...");
            OverlayShaders.EnsureLoaded();
            _pipeline = new OverlayPipeline(
                ctx.Graphics,
                cmds.FrameContext.RenderPass,
                OverlayShaders.Vertex,
                OverlayShaders.Fragment);
            Logger.Info("Overlay pipeline created.");
        }

        var cmd = cmds.FrameContext.CommandBuffer;
        var extent = cmds.FrameContext.Extent;

        ctx.Graphics.BindGraphicsPipeline(cmd, _pipeline.Pipeline);

        // Dynamic viewport + scissor are required — the pipeline uses dynamic state
        ctx.Graphics.SetViewport(cmd, 0, 0, extent.Width, extent.Height, 0, 1);
        ctx.Graphics.SetScissor(cmd, 0, 0, extent.Width, extent.Height);

        ctx.Graphics.BindDescriptorSet(cmd, _pipeline.Pipeline, overlay.DescriptorSet);
        ctx.Graphics.Draw(cmd, vertexCount: 3);
    }
}

/// <summary>Resource stored in the RenderWorld holding the overlay texture's descriptor set.</summary>
public sealed class OverlayTexture
{
    public IDescriptorSet? DescriptorSet { get; set; }
}
