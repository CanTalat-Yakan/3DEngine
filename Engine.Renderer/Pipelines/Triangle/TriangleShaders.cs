namespace Engine;

/// <summary>Loads and optionally compiles triangle sample SPIR-V shaders from GLSL sources.</summary>
public static class TriangleShaders
{
    private static readonly ILogger Logger = Log.Category("Engine.Renderer.Triangle");

    private static bool _loaded;

    public static ReadOnlyMemory<byte> Vertex { get; private set; } = ReadOnlyMemory<byte>.Empty;
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

        if (File.Exists(vertSpvPath) && File.Exists(fragSpvPath))
        {
            Logger.Debug($"Found pre-compiled SPIR-V: {vertSpvPath}, {fragSpvPath}");
            Vertex = File.ReadAllBytes(vertSpvPath);
            Fragment = File.ReadAllBytes(fragSpvPath);
            _loaded = true;
            Logger.Info($"Triangle shaders loaded from SPIR-V (vertex={Vertex.Length} bytes, fragment={Fragment.Length} bytes).");
            return;
        }

        if (File.Exists(vertGlslPath) && File.Exists(fragGlslPath))
        {
            Logger.Info($"SPIR-V not found — compiling GLSL with glslc: {vertGlslPath}, {fragGlslPath}");
            TryCompileWithGlslc(vertGlslPath, vertSpvPath, isVertex: true);
            TryCompileWithGlslc(fragGlslPath, fragSpvPath, isVertex: false);

            if (File.Exists(vertSpvPath) && File.Exists(fragSpvPath))
            {
                Vertex = File.ReadAllBytes(vertSpvPath);
                Fragment = File.ReadAllBytes(fragSpvPath);
                _loaded = true;
                Logger.Info($"Triangle shaders compiled and loaded (vertex={Vertex.Length} bytes, fragment={Fragment.Length} bytes).");
                return;
            }

            Logger.Error("glslc compilation did not produce SPIR-V output files.");
        }
        else
        {
            Logger.Error($"Shader source files not found: {vertGlslPath}, {fragGlslPath}");
        }

        throw new InvalidOperationException("Triangle shaders could not be loaded. Ensure SPIR-V files exist or glslc is available.");
    }

    private static void TryCompileWithGlslc(string glslPath, string spvPath, bool isVertex)
    {
        try
        {
            var glslc = "glslc";
            var argsStage = isVertex ? "-fshader-stage=vert" : "-fshader-stage=frag";
            Logger.Debug($"Running: {glslc} {argsStage} \"{glslPath}\" -o \"{spvPath}\"");

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
            {
                Logger.Warn("Failed to start glslc process.");
                return;
            }
            var stderr = proc.StandardError.ReadToEnd();
            proc.WaitForExit();

            if (proc.ExitCode != 0)
                Logger.Error($"glslc exited with code {proc.ExitCode}: {stderr.Trim()}");
            else
                Logger.Debug($"glslc compiled {Path.GetFileName(glslPath)} → {Path.GetFileName(spvPath)}");
        }
        catch (Exception ex)
        {
            Logger.Warn($"glslc not available or failed: {ex.Message}");
        }
    }
}
