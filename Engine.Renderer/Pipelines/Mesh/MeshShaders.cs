namespace Engine;

/// <summary>Loads and compiles mesh rendering SPIR-V shaders from GLSL sources.</summary>
/// <remarks>
/// Uses <see cref="GlslCompiler.CompileFileWithCache"/> to skip recompilation when
/// a fresh <c>.spv</c> file already exists on disk. Call <see cref="EnsureLoaded"/>
/// once before creating a <see cref="MeshPipeline"/>.
/// </remarks>
/// <seealso cref="MeshPipeline"/>
/// <seealso cref="GlslCompiler"/>
public static class MeshShaders
{
    private static readonly ILogger Logger = Log.Category("Engine.Renderer.Mesh");

    private static bool _loaded;

    /// <summary>Compiled SPIR-V bytecode for the mesh vertex shader.</summary>
    public static ReadOnlyMemory<byte> Vertex { get; private set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>Compiled SPIR-V bytecode for the mesh fragment shader.</summary>
    public static ReadOnlyMemory<byte> Fragment { get; private set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>Ensures that the mesh shaders are loaded and compiled. Idempotent.</summary>
    public static void EnsureLoaded()
    {
        if (_loaded) return;

        Logger.Info("Loading mesh shaders...");
        var baseDir = AppContext.BaseDirectory;
        var vertGlslPath = Path.Combine(baseDir, "source", "shaders", "mesh.vert.glsl");
        var fragGlslPath = Path.Combine(baseDir, "source", "shaders", "mesh.frag.glsl");
        var vertSpvPath = Path.ChangeExtension(vertGlslPath, ".vert.spv");
        var fragSpvPath = Path.ChangeExtension(fragGlslPath, ".frag.spv");

        try
        {
            Vertex = GlslCompiler.CompileFileWithCache(vertGlslPath, vertSpvPath, ShaderStage.Vertex);
            Fragment = GlslCompiler.CompileFileWithCache(fragGlslPath, fragSpvPath, ShaderStage.Fragment);
            _loaded = true;
            Logger.Info($"Mesh shaders loaded (vertex={Vertex.Length} bytes, fragment={Fragment.Length} bytes).");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load mesh shaders: {ex.Message}");
            throw new InvalidOperationException(
                "Mesh shaders could not be loaded. Ensure GLSL sources or pre-compiled SPIR-V files exist.", ex);
        }
    }
}

