namespace Engine;

/// <summary>Loads and optionally compiles overlay SPIR-V shaders from GLSL sources.</summary>
public static class OverlayShaders
{
    private static bool _loaded;

    public static ReadOnlyMemory<byte> Vertex { get; private set; } = ReadOnlyMemory<byte>.Empty;
    public static ReadOnlyMemory<byte> Fragment { get; private set; } = ReadOnlyMemory<byte>.Empty;

    /// <summary>Ensures that the overlay shaders are loaded and compiled. Idempotent.</summary>
    public static void EnsureLoaded()
    {
        if (_loaded) return;

        var baseDir = AppContext.BaseDirectory;
        var vertGlslPath = Path.Combine(baseDir, "Source", "Shaders", "overlay.vert.glsl");
        var fragGlslPath = Path.Combine(baseDir, "Source", "Shaders", "overlay.frag.glsl");
        var vertSpvPath = Path.ChangeExtension(vertGlslPath, ".vert.spv");
        var fragSpvPath = Path.ChangeExtension(fragGlslPath, ".frag.spv");

        if (File.Exists(vertSpvPath) && File.Exists(fragSpvPath))
        {
            Vertex = File.ReadAllBytes(vertSpvPath);
            Fragment = File.ReadAllBytes(fragSpvPath);
            _loaded = true;
            return;
        }

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

        throw new InvalidOperationException("Overlay shaders could not be loaded. Ensure SPIR-V files exist or glslc is available.");
    }

    private static void TryCompileWithGlslc(string glslPath, string spvPath, bool isVertex)
    {
        try
        {
            var glslc = "glslc";
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
            if (proc is null) return;
            proc.WaitForExit();
        }
        catch
        {
        }
    }
}

