namespace Engine;

/// <summary>Loads and optionally compiles triangle sample SPIR-V shaders from GLSL sources.</summary>
/// <remarks>
/// Follows the same caching pattern as <see cref="ImGuiShaders"/>:
/// uses <see cref="GlslCompiler.CompileFileWithCache"/> to skip recompilation when a fresh
/// <c>.spv</c> file already exists on disk.  Call <see cref="EnsureLoaded"/> once from the
/// main thread during Startup.
/// </remarks>
/// <seealso cref="TrianglePipeline"/>
/// <seealso cref="GlslCompiler"/>
public static class TriangleShaders
{
    private static readonly ILogger Logger = Log.Category("Engine.Renderer.Triangle");

    private static bool _loaded;

    /// <summary>Compiled SPIR-V bytecode for the triangle vertex shader.</summary>
    public static ReadOnlyMemory<byte> Vertex { get; private set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>Compiled SPIR-V bytecode for the triangle fragment shader.</summary>
    public static ReadOnlyMemory<byte> Fragment { get; private set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>Ensures that the triangle shaders are loaded and compiled. Idempotent.</summary>
    public static void EnsureLoaded()
    {
        if (_loaded) return;

        Logger.Info("Loading triangle shaders...");
        var baseDir = AppContext.BaseDirectory;
        var vertGlslPath = Path.Combine(baseDir, "source", "shaders", "triangle.vert.glsl");
        var fragGlslPath = Path.Combine(baseDir, "source", "shaders", "triangle.frag.glsl");
        var vertSpvPath = Path.ChangeExtension(vertGlslPath, ".vert.spv");
        var fragSpvPath = Path.ChangeExtension(fragGlslPath, ".frag.spv");

        try
        {
            Vertex = GlslCompiler.CompileFileWithCache(vertGlslPath, vertSpvPath, ShaderStage.Vertex);
            Fragment = GlslCompiler.CompileFileWithCache(fragGlslPath, fragSpvPath, ShaderStage.Fragment);
            _loaded = true;
            Logger.Info($"Triangle shaders loaded (vertex={Vertex.Length} bytes, fragment={Fragment.Length} bytes).");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load triangle shaders: {ex.Message}");
            throw new InvalidOperationException("Triangle shaders could not be loaded. Ensure GLSL sources or pre-compiled SPIR-V files exist.", ex);
        }
    }
}
