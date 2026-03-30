namespace Engine;

/// <summary>Creates and owns a full-screen overlay graphics pipeline with alpha blending.</summary>
public sealed class OverlayPipeline : IDisposable
{
    private readonly IShader _vertexShader;
    private readonly IShader _fragmentShader;
    private readonly IPipeline _pipeline;

    public IPipeline Pipeline => _pipeline;

    public OverlayPipeline(IGraphicsDevice graphics, IRenderPass renderPass,
        ReadOnlyMemory<byte> vertexSpirv, ReadOnlyMemory<byte> fragmentSpirv)
    {
        var vsDesc = new ShaderDesc(ShaderStage.Vertex, vertexSpirv, "main");
        var fsDesc = new ShaderDesc(ShaderStage.Fragment, fragmentSpirv, "main");

        _vertexShader = graphics.CreateShader(vsDesc);
        _fragmentShader = graphics.CreateShader(fsDesc);

        var pipelineDesc = new GraphicsPipelineDesc(
            renderPass, _vertexShader, _fragmentShader,
            BlendEnabled: true,
            CullBackFace: false);

        _pipeline = graphics.CreateGraphicsPipeline(pipelineDesc);
    }

    public void Dispose()
    {
        _fragmentShader.Dispose();
        _vertexShader.Dispose();
    }
}

