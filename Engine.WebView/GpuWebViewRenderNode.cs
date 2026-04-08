namespace Engine;

/// <summary>
/// Render graph node for GPU-accelerated Ultralight rendering.
/// Instead of reading CPU bitmap pixels and uploading them (like <see cref="WebViewRenderNode"/>),
/// this node executes Ultralight's deferred GPU command list via the <see cref="VulkanUltralightGpuDriver"/>,
/// then composites the resulting texture to screen using the same fullscreen-triangle technique.
/// </summary>
/// <remarks>
/// <para>
/// On each frame:
/// <list type="number">
///   <item><description>Flushes pending Ultralight GPU commands (geometry draws into offscreen textures).</description></item>
///   <item><description>Reads the View's <c>RenderTarget.TextureId</c> to find the final composited texture.</description></item>
///   <item><description>Draws a fullscreen triangle sampling that texture with alpha blending.</description></item>
/// </list>
/// </para>
/// </remarks>
/// <seealso cref="WebViewRenderNode"/>
/// <seealso cref="VulkanUltralightGpuDriver"/>
/// <seealso cref="WebViewInstance"/>
public sealed class GpuWebViewRenderNode : IRenderNode, IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.GpuNode");

    /// <inheritdoc />
    public string Name => "webview";

    /// <inheritdoc />
    public IReadOnlyCollection<string> Dependencies { get; } = new[] { "sample" };

    // Fullscreen composite resources
    private IPipeline? _compositePipeline;
    private IShader? _compositeVs;
    private IShader? _compositeFs;
    private ISampler? _sampler;
    private IDescriptorSet? _descriptorSet;
    private uint _lastBoundTextureId;

    /// <inheritdoc />
    public void Execute(RendererContext ctx, CommandRecordingContext cmds, RenderWorld renderWorld)
    {
        var webview = renderWorld.TryGet<WebViewInstance>();
        if (webview?.View is null) return;

        var gpuDriver = renderWorld.TryGet<VulkanUltralightGpuDriver>();
        if (gpuDriver is null) return;

        var gfx = ctx.Graphics;
        var cmd = cmds.FrameContext.CommandBuffer;
        var extent = cmds.FrameContext.Extent;
        var renderPass = cmds.FrameContext.RenderPass;

        // ── Execute Ultralight GPU commands ──────────────────────────
        if (gpuDriver.HasPendingCommands)
        {
            gpuDriver.FlushCommands(cmd, renderPass);
        }

        // ── Composite the view's render target to screen ────────────
        if (!webview.View.IsAccelerated) return;

        var rt = webview.View.RenderTarget;
        if (rt.IsEmpty || rt.TextureId == 0) return;

        // Lazy-init composite pipeline
        if (_compositePipeline is null)
            CreateCompositePipeline(gfx, renderPass);

        // Update descriptor set if the texture changed
        if (_lastBoundTextureId != rt.TextureId)
        {
            var texView = gpuDriver.GetTextureView(rt.TextureId);
            if (texView is null)
            {
                Logger.Warn($"GPU render target texture id={rt.TextureId} not found in driver.");
                return;
            }

            _descriptorSet?.Dispose();
            _descriptorSet = gfx.CreateDescriptorSet();

            _sampler?.Dispose();
            _sampler = gfx.CreateSampler(new SamplerDesc(
                SamplerFilter.Linear, SamplerFilter.Linear,
                SamplerAddressMode.ClampToEdge,
                SamplerAddressMode.ClampToEdge,
                SamplerAddressMode.ClampToEdge));

            var binding = new CombinedImageSamplerBinding(texView, _sampler, 1);
            gfx.UpdateDescriptorSet(_descriptorSet, uniformBinding: null, binding);
            _lastBoundTextureId = rt.TextureId;
        }

        if (_compositePipeline is null || _descriptorSet is null)
            return;

        // Set viewport and scissor to full framebuffer
        gfx.SetViewport(cmd, 0, 0, extent.Width, extent.Height, 0, 1);
        gfx.SetScissor(cmd, 0, 0, extent.Width, extent.Height);

        // Bind composite pipeline and draw fullscreen triangle
        gfx.BindGraphicsPipeline(cmd, _compositePipeline);
        gfx.BindDescriptorSet(cmd, _compositePipeline, _descriptorSet);
        gfx.Draw(cmd, vertexCount: 3, instanceCount: 1);
    }

    private void CreateCompositePipeline(IGraphicsDevice gfx, IRenderPass renderPass)
    {
        Logger.Info("Creating GPU webview composite pipeline...");

        // Reuse the same fullscreen overlay shaders as the CPU path
        WebViewShaders.EnsureLoaded();
        _compositeVs = gfx.CreateShader(new ShaderDesc(ShaderStage.Vertex, WebViewShaders.Vertex, "main"));
        _compositeFs = gfx.CreateShader(new ShaderDesc(ShaderStage.Fragment, WebViewShaders.Fragment, "main"));

        _compositePipeline = gfx.CreateGraphicsPipeline(new GraphicsPipelineDesc(
            renderPass,
            _compositeVs,
            _compositeFs,
            BlendEnabled: true,
            CullBackFace: false,
            VertexBindings: null,
            VertexAttributes: null,
            PushConstantRanges: null));

        Logger.Info("GPU webview composite pipeline created.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Logger.Info("Disposing GpuWebViewRenderNode...");
        _descriptorSet?.Dispose();
        _sampler?.Dispose();
        _compositeFs?.Dispose();
        _compositeVs?.Dispose();
    }
}

