namespace Engine;

/// <summary>
/// Loader for triangle sample SPIR-V shaders. At runtime, this attempts to find precompiled
/// SPIR-V files next to the GLSL sources; if they are missing, it optionally invokes an
/// external 'glslc' compiler (if available on PATH) to compile the GLSL files, then caches
/// the results. Finally, the compiled bytecode is exposed via the Vertex/Fragment properties.
/// </summary>
public static class TriangleShaders
{
    private static bool _loaded;

    public static ReadOnlyMemory<byte> Vertex { get; private set; } = ReadOnlyMemory<byte>.Empty;
    public static ReadOnlyMemory<byte> Fragment { get; private set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>
    /// Ensure that the triangle shaders are loaded and compiled. This method is idempotent.
    /// </summary>
    public static void EnsureLoaded()
    {
        if (_loaded) return;

        var baseDir = AppContext.BaseDirectory;
        var vertGlslPath = Path.Combine(baseDir, "Source", "Shaders", "triangle.vert.glsl");
        var fragGlslPath = Path.Combine(baseDir, "Source", "Shaders", "triangle.frag.glsl");
        var vertSpvPath = Path.ChangeExtension(vertGlslPath, ".vert.spv");
        var fragSpvPath = Path.ChangeExtension(fragGlslPath, ".frag.spv");

        // If precompiled SPIR-V exists, just load it.
        if (File.Exists(vertSpvPath) && File.Exists(fragSpvPath))
        {
            Vertex = File.ReadAllBytes(vertSpvPath);
            Fragment = File.ReadAllBytes(fragSpvPath);
            _loaded = true;
            return;
        }

        // Fallback: try to compile GLSL to SPIR-V using 'glslc' if available.
        if (File.Exists(vertGlslPath) && File.Exists(fragGlslPath))
        {
            TryCompileWithGlslc(vertGlslPath, vertSpvPath, isVertex: true);
            TryCompileWithGlslc(fragGlslPath, fragSpvPath, isVertex: false);

            if (File.Exists(vertSpvPath) && File.Exists(fragSpvPath))
            {
                Vertex = File.ReadAllBytes(vertSpvPath);
                Fragment = File.ReadAllBytes(fragSpvPath);
                _loaded = true;
                return;
            }
        }

        throw new InvalidOperationException("Triangle shaders could not be loaded. Ensure SPIR-V files exist or glslc is available.");
    }

    private static void TryCompileWithGlslc(string glslPath, string spvPath, bool isVertex)
    {
        try
        {
            var glslc = "glslc"; // assumes glslc is on PATH
            var argsStage = isVertex ? "-fshader-stage=vert" : "-fshader-stage=frag";
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = glslc,
                Arguments = $"{argsStage} \"{glslPath}\" -o \"{spvPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using var proc = System.Diagnostics.Process.Start(psi);
            if (proc is null)
                return;
            proc.WaitForExit();
            // On failure, leave it to EnsureLoaded to throw if SPIR-V is still missing.
        }
        catch
        {
            // Ignore errors; caller will detect missing SPIR-V.
        }
    }
}
