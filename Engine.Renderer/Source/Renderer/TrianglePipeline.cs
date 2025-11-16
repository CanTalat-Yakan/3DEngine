namespace Engine;

/// <summary>
/// Helper that creates and owns a simple triangle graphics pipeline based on provided SPIR-V shaders.
/// The pipeline layout is compatible with the global camera descriptor set layout used by the Vulkan backend.
/// </summary>
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
        // IPipeline is not disposable in the public abstraction; the underlying graphics device
        // is responsible for cleaning up pipelines when disposed. We only own the shaders here.
        _fragmentShader.Dispose();
        _vertexShader.Dispose();
    }
}
