namespace Engine;

/// <summary>Creates and owns a simple triangle graphics pipeline from SPIR-V shaders.</summary>
public sealed class TrianglePipeline : IDisposable
{
    private readonly IGraphicsDevice _graphics;
    private readonly IShader _vertexShader;
    private readonly IShader _fragmentShader;
    private readonly IPipeline _pipeline;

    public IPipeline Pipeline => _pipeline;

    public TrianglePipeline(IGraphicsDevice graphics, IRenderPass renderPass, ReadOnlyMemory<byte> vertexSpirv, ReadOnlyMemory<byte> fragmentSpirv)
    {
        _graphics = graphics;

        var vsDesc = new ShaderDesc(ShaderStage.Vertex, vertexSpirv, "main");
        var fsDesc = new ShaderDesc(ShaderStage.Fragment, fragmentSpirv, "main");

        _vertexShader = _graphics.CreateShader(vsDesc);
        _fragmentShader = _graphics.CreateShader(fsDesc);

        var pipelineDesc = new GraphicsPipelineDesc(renderPass, _vertexShader, _fragmentShader);
        _pipeline = _graphics.CreateGraphicsPipeline(pipelineDesc);
    }

    public void Dispose()
    {
        _fragmentShader.Dispose();
        _vertexShader.Dispose();
    }
}
