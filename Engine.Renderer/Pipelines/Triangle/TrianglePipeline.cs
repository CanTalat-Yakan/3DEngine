namespace Engine;

/// <summary>Creates and owns a simple triangle graphics pipeline from SPIR-V shaders.</summary>
/// <remarks>
/// The pipeline uses no vertex input (the fullscreen triangle is generated in the vertex shader),
/// no blending, and no back-face culling.  It is created once and reused for every frame.
/// </remarks>
/// <seealso cref="TriangleShaders"/>
public sealed class TrianglePipeline : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.Renderer.Triangle");

    private readonly IGraphicsDevice _graphics;
    private readonly IShader _vertexShader;
    private readonly IShader _fragmentShader;
    private readonly IPipeline _pipeline;

    /// <summary>The compiled Vulkan graphics pipeline handle.</summary>
    public IPipeline Pipeline => _pipeline;

    /// <summary>Creates a new triangle pipeline from pre-compiled SPIR-V bytecode.</summary>
    /// <param name="graphics">The graphics device to create GPU resources on.</param>
    /// <param name="renderPass">The render pass the pipeline must be compatible with.</param>
    /// <param name="vertexSpirv">SPIR-V bytecode for the vertex shader.</param>
    /// <param name="fragmentSpirv">SPIR-V bytecode for the fragment shader.</param>
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

    /// <summary>Disposes the vertex and fragment shader modules.</summary>
    public void Dispose()
    {
        Logger.Debug("Disposing triangle pipeline shaders...");
        _fragmentShader.Dispose();
        _vertexShader.Dispose();
    }
}
