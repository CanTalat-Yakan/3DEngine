namespace Engine;

/// <summary>
/// Loads and caches the Ultralight GPU driver SPIR-V shaders (Fill and FillPath programs).
/// Used by <see cref="VulkanUltralightGpuDriver"/> when running in GPU-accelerated mode.
/// </summary>
/// <seealso cref="WebViewShaders"/>
/// <seealso cref="GlslCompiler"/>
public static class UltralightGpuShaders
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.GpuShaders");

    private static bool _loaded;

    /// <summary>SPIR-V bytecode for the Fill vertex shader (quad rendering).</summary>
    public static ReadOnlyMemory<byte> FillVertex { get; private set; } = ReadOnlyMemory<byte>.Empty;
    /// <summary>SPIR-V bytecode for the Fill fragment shader (quad rendering).</summary>
    public static ReadOnlyMemory<byte> FillFragment { get; private set; } = ReadOnlyMemory<byte>.Empty;
    /// <summary>SPIR-V bytecode for the FillPath vertex shader (path rendering).</summary>
    public static ReadOnlyMemory<byte> FillPathVertex { get; private set; } = ReadOnlyMemory<byte>.Empty;
    /// <summary>SPIR-V bytecode for the FillPath fragment shader (path rendering).</summary>
    public static ReadOnlyMemory<byte> FillPathFragment { get; private set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>Ensures all Ultralight GPU shaders are loaded and compiled. Idempotent.</summary>
    public static void EnsureLoaded()
    {
        if (_loaded) return;

        Logger.Info("Loading Ultralight GPU driver shaders...");
        var baseDir = AppContext.BaseDirectory;
        var shaderDir = Path.Combine(baseDir, "source", "shaders");

        try
        {
            FillVertex = CompileShader(shaderDir, "ul_fill.vert.glsl", ShaderStage.Vertex);
            FillFragment = CompileShader(shaderDir, "ul_fill.frag.glsl", ShaderStage.Fragment);
            FillPathVertex = CompileShader(shaderDir, "ul_fill_path.vert.glsl", ShaderStage.Vertex);
            FillPathFragment = CompileShader(shaderDir, "ul_fill_path.frag.glsl", ShaderStage.Fragment);
            _loaded = true;

            Logger.Info($"Ultralight GPU shaders loaded (Fill vert={FillVertex.Length}B frag={FillFragment.Length}B, " +
                        $"FillPath vert={FillPathVertex.Length}B frag={FillPathFragment.Length}B).");
        }
        catch (Exception ex)
        {
            Logger.Error($"Failed to load Ultralight GPU shaders: {ex.Message}");
            throw new InvalidOperationException(
                "Ultralight GPU shaders could not be loaded. Ensure GLSL sources or pre-compiled SPIR-V files exist.", ex);
        }
    }

    private static ReadOnlyMemory<byte> CompileShader(string dir, string glslFile, ShaderStage stage)
    {
        var glslPath = Path.Combine(dir, glslFile);
        var spvExt = stage == ShaderStage.Vertex ? ".vert.spv" : ".frag.spv";
        var spvPath = Path.Combine(dir, Path.GetFileNameWithoutExtension(Path.GetFileNameWithoutExtension(glslFile)) + spvExt);
        return GlslCompiler.CompileFileWithCache(glslPath, spvPath, stage);
    }
}

