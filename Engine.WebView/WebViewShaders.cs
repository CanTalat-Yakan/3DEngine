namespace Engine;

/// <summary>Loads and optionally compiles webview overlay SPIR-V shaders from GLSL sources.</summary>
public static class WebViewShaders
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.Shaders");

    private static bool _loaded;

    public static ReadOnlyMemory<byte> Vertex { get; private set; } = ReadOnlyMemory<byte>.Empty;
    public static ReadOnlyMemory<byte> Fragment { get; private set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>Ensures that the webview overlay shaders are loaded and compiled. Idempotent.</summary>
    public static void EnsureLoaded()
    {
        if (_loaded) return;

        Logger.Info("Loading webview overlay shaders...");
        var baseDir = AppContext.BaseDirectory;
        var vertGlslPath = Path.Combine(baseDir, "source", "shaders", "webview.vert.glsl");
        var fragGlslPath = Path.Combine(baseDir, "source", "shaders", "webview.frag.glsl");
        var vertSpvPath = Path.ChangeExtension(vertGlslPath, ".vert.spv");
        var fragSpvPath = Path.ChangeExtension(fragGlslPath, ".frag.spv");

        try
        {
            Vertex = GlslCompiler.CompileFileWithCache(vertGlslPath, vertSpvPath, ShaderStage.Vertex);
            Fragment = GlslCompiler.CompileFileWithCache(fragGlslPath, fragSpvPath, ShaderStage.Fragment);
            _loaded = true;
            Logger.Info($"WebView shaders loaded (vertex={Vertex.Length} bytes, fragment={Fragment.Length} bytes).");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load webview shaders: {ex.Message}");
            throw new InvalidOperationException("WebView shaders could not be loaded. Ensure GLSL sources or pre-compiled SPIR-V files exist.", ex);
        }
    }
}
