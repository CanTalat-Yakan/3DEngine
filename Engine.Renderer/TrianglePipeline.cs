namespace Engine;

/// <summary>Creates and owns a simple triangle graphics pipeline from SPIR-V shaders.</summary>
public sealed class TrianglePipeline : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.Renderer.Triangle");

    private readonly IGraphicsDevice _graphics;
    private readonly IShader _vertexShader;
    private readonly IShader _fragmentShader;
    private readonly IPipeline _pipeline;

    public IPipeline Pipeline => _pipeline;

    public TrianglePipeline(IGraphicsDevice graphics, IRenderPass renderPass, ReadOnlyMemory<byte> vertexSpirv, ReadOnlyMemory<byte> fragmentSpirv)
    {
        _graphics = graphics;

        Logger.Debug($"Compiling triangle shaders (vertex={vertexSpirv.Length} bytes, fragment={fragmentSpirv.Length} bytes)...");
        var vsDesc = new ShaderDesc(ShaderStage.Vertex, vertexSpirv, "main");
        var fsDesc = new ShaderDesc(ShaderStage.Fragment, fragmentSpirv, "main");

        _vertexShader = _graphics.CreateShader(vsDesc);
        _fragmentShader = _graphics.CreateShader(fsDesc);

        Logger.Debug("Creating triangle graphics pipeline...");
        var pipelineDesc = new GraphicsPipelineDesc(renderPass, _vertexShader, _fragmentShader);
        _pipeline = _graphics.CreateGraphicsPipeline(pipelineDesc);
        Logger.Debug("Triangle pipeline created successfully.");
    }

    public void Dispose()
    {
        Logger.Debug("Disposing triangle pipeline shaders...");
        _fragmentShader.Dispose();
        _vertexShader.Dispose();
    }
}
