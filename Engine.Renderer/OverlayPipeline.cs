namespace Engine;

/// <summary>Creates and owns a full-screen overlay graphics pipeline with alpha blending.</summary>
public sealed class OverlayPipeline : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.Renderer.Overlay");

    private readonly IShader _vertexShader;
    private readonly IShader _fragmentShader;
    private readonly IPipeline _pipeline;

    public IPipeline Pipeline => _pipeline;

    public OverlayPipeline(IGraphicsDevice graphics, IRenderPass renderPass,
        ReadOnlyMemory<byte> vertexSpirv, ReadOnlyMemory<byte> fragmentSpirv)
    {
        Logger.Debug($"Compiling overlay shaders (vertex={vertexSpirv.Length} bytes, fragment={fragmentSpirv.Length} bytes)...");

        var vsDesc = new ShaderDesc(ShaderStage.Vertex, vertexSpirv, "main");
        var fsDesc = new ShaderDesc(ShaderStage.Fragment, fragmentSpirv, "main");

        _vertexShader = graphics.CreateShader(vsDesc);
        _fragmentShader = graphics.CreateShader(fsDesc);

        Logger.Debug("Creating overlay graphics pipeline (blend=on, cull=off)...");
        var pipelineDesc = new GraphicsPipelineDesc(
            renderPass, _vertexShader, _fragmentShader,
            BlendEnabled: true,
            CullBackFace: false);

        _pipeline = graphics.CreateGraphicsPipeline(pipelineDesc);
        Logger.Debug("Overlay pipeline created successfully.");
    }

    public void Dispose()
    {
        Logger.Debug("Disposing overlay pipeline shaders...");
        _fragmentShader.Dispose();
        _vertexShader.Dispose();
    }
}
