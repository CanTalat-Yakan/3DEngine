using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Editor.Shell;

/// <summary>
/// Runtime Roslyn compiler that watches a scripts directory, compiles .cs files
/// on change, loads the resulting assembly into an isolated <see cref="AssemblyLoadContext"/>,
/// discovers <see cref="IEditorShellBuilder"/> implementations, and pushes the
/// rebuilt <see cref="ShellDescriptor"/> into the <see cref="ShellRegistry"/>.
/// </summary>
public sealed class ScriptCompiler : IDisposable
{
    private readonly ShellRegistry _registry;
    private readonly List<string> _scriptDirectories = [];
    private readonly List<MetadataReference> _references = [];
    private readonly List<FileSystemWatcher> _watchers = [];
    private readonly Lock _compileLock = new();

    private Timer? _debounceTimer;
    private ScriptLoadContext? _currentContext;
    private int _generation;

    /// <summary>Fired when compilation completes (success or failure).</summary>
    public event Action<ScriptCompilationResult>? CompilationCompleted;

    public ScriptCompiler(ShellRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        AddDefaultReferences();
    }

    // ── Configuration ───────────────────────────────────────────────────

    /// <summary>Adds a directory to watch for .cs script files.</summary>
    public ScriptCompiler WatchDirectory(string path)
    {
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);

        _scriptDirectories.Add(Path.GetFullPath(path));
        return this;
    }

    /// <summary>Adds assemblies to the compilation reference set.</summary>
    public ScriptCompiler AddReference(Assembly assembly)
    {
        var location = assembly.Location;
        if (!string.IsNullOrEmpty(location) && File.Exists(location))
            _references.Add(MetadataReference.CreateFromFile(location));
        return this;
    }

    /// <summary>Adds a metadata reference directly.</summary>
    public ScriptCompiler AddReference(MetadataReference reference)
    {
        _references.Add(reference);
        return this;
    }

    // ── Lifecycle ───────────────────────────────────────────────────────

    /// <summary>
    /// Performs an initial compilation and starts file watchers.
    /// Call once after configuration.
    /// </summary>
    public ScriptCompilationResult Start()
    {
        var result = CompileAndLoad();

        foreach (var dir in _scriptDirectories)
        {
            var watcher = new FileSystemWatcher(dir, "*.cs")
            {
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.CreationTime,
                IncludeSubdirectories = true,
                EnableRaisingEvents = true
            };
            watcher.Changed += OnFileChanged;
            watcher.Created += OnFileChanged;
            watcher.Deleted += OnFileChanged;
            watcher.Renamed += OnFileRenamed;
            _watchers.Add(watcher);
        }

        return result;
    }

    /// <summary>Manually triggers a recompilation.</summary>
    public ScriptCompilationResult Recompile() => CompileAndLoad();

    public void Dispose()
    {
        _debounceTimer?.Dispose();
        foreach (var w in _watchers)
        {
            w.EnableRaisingEvents = false;
            w.Dispose();
        }
        _watchers.Clear();
        UnloadCurrent();
    }

    // ── File Watcher Events ─────────────────────────────────────────────

    private void OnFileChanged(object sender, FileSystemEventArgs e) => ScheduleRecompile();
    private void OnFileRenamed(object sender, RenamedEventArgs e) => ScheduleRecompile();

    private void ScheduleRecompile()
    {
        _debounceTimer?.Dispose();
        _debounceTimer = new Timer(_ =>
        {
            var result = CompileAndLoad();
            CompilationCompleted?.Invoke(result);
        }, null, 300, Timeout.Infinite);
    }

    // ── Compilation ─────────────────────────────────────────────────────

    private ScriptCompilationResult CompileAndLoad()
    {
        lock (_compileLock)
        {
            var result = new ScriptCompilationResult();

            // Collect all script files
            var files = new List<string>();
            foreach (var dir in _scriptDirectories)
            {
                if (Directory.Exists(dir))
                    files.AddRange(Directory.GetFiles(dir, "*.cs", SearchOption.AllDirectories));
            }

            if (files.Count == 0)
            {
                result.Success = true;
                result.Message = "No script files found.";
                // Push an empty shell descriptor so the UI is valid.
                _registry.Update(new ShellDescriptor(), []);
                return result;
            }

            result.Files = files.Select(Path.GetFileName).ToArray()!;

            // Parse
            var syntaxTrees = new List<SyntaxTree>();
            foreach (var file in files)
            {
                try
                {
                    var source = File.ReadAllText(file);
                    var tree = CSharpSyntaxTree.ParseText(source, path: file,
                        options: new CSharpParseOptions(LanguageVersion.Latest));
                    syntaxTrees.Add(tree);
                }
                catch (Exception ex)
                {
                    result.Errors.Add(new ScriptError
                    {
                        FileName = Path.GetFileName(file),
                        Message = $"Failed to read: {ex.Message}"
                    });
                }
            }

            if (result.Errors.Count > 0)
            {
                result.Success = false;
                result.Message = "Parse errors.";
                return result;
            }

            // Compile
            var gen = Interlocked.Increment(ref _generation);
            var compilation = CSharpCompilation.Create(
                $"EditorScripts_Gen{gen}",
                syntaxTrees,
                _references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary)
                    .WithOptimizationLevel(OptimizationLevel.Debug)
                    .WithAllowUnsafe(true));

            using var ms = new MemoryStream();
            var emitResult = compilation.Emit(ms);

            if (!emitResult.Success)
            {
                foreach (var diag in emitResult.Diagnostics)
                {
                    if (diag.Severity == DiagnosticSeverity.Error)
                    {
                        var lineSpan = diag.Location.GetMappedLineSpan();
                        result.Errors.Add(new ScriptError
                        {
                            FileName = Path.GetFileName(lineSpan.Path ?? ""),
                            Message = diag.GetMessage(),
                            Line = lineSpan.StartLinePosition.Line + 1,
                            Column = lineSpan.StartLinePosition.Character + 1
                        });
                    }
                }

                result.Success = false;
                result.Message = $"Compilation failed with {result.Errors.Count} error(s).";
                return result;
            }

            // Load into isolated context
            ms.Position = 0;
            UnloadCurrent();

            var loadContext = new ScriptLoadContext($"Scripts_Gen{gen}");
            var assembly = loadContext.LoadFromStream(ms);
            _currentContext = loadContext;

            // Discover and execute shell builders
            var shellDescriptor = DiscoverAndBuildShell(assembly, result);

            // Discover inspectable component metadata
            var components = DiscoverComponents(assembly);

            // Push to registry
            _registry.Update(shellDescriptor, components);

            result.Success = true;
            result.Message = $"Compiled {files.Count} file(s) successfully (gen {gen}).";
            return result;
        }
    }

    private ShellDescriptor DiscoverAndBuildShell(Assembly assembly, ScriptCompilationResult result)
    {
        var builders = new List<IEditorShellBuilder>();

        foreach (var type in assembly.GetExportedTypes())
        {
            if (type.GetCustomAttribute<EditorShellAttribute>() == null) continue;
            if (!typeof(IEditorShellBuilder).IsAssignableFrom(type)) continue;

            try
            {
                if (Activator.CreateInstance(type) is IEditorShellBuilder builder)
                    builders.Add(builder);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Failed to instantiate {type.Name}: {ex.Message}");
            }
        }

        // Sort by Order
        builders.Sort((a, b) => a.Order.CompareTo(b.Order));

        // Build the descriptor by running all builders against a shared ShellBuilder
        var shellBuilder = new ShellBuilder();
        foreach (var builder in builders)
        {
            try
            {
                builder.Build(shellBuilder);
            }
            catch (Exception ex)
            {
                result.Warnings.Add($"Builder {builder.GetType().Name}.Build() failed: {ex.Message}");
            }
        }

        return shellBuilder.Build();
    }

    private static List<InspectableComponentDescriptor> DiscoverComponents(Assembly assembly)
    {
        var result = new List<InspectableComponentDescriptor>();

        foreach (var type in assembly.GetExportedTypes())
        {
            // Look for types that have fields with [Field] attribute
            var fields = new List<ComponentFieldDescriptor>();
            foreach (var field in type.GetFields(BindingFlags.Public | BindingFlags.Instance))
            {
                var fieldAttr = field.GetCustomAttribute<FieldAttribute>();
                var hideAttr = field.GetCustomAttribute<HideInInspectorAttribute>();

                if (hideAttr != null)
                {
                    fields.Add(new ComponentFieldDescriptor
                    {
                        FieldName = field.Name,
                        Label = field.Name,
                        TypeName = field.FieldType.Name,
                        Hidden = true
                    });
                    continue;
                }

                if (fieldAttr == null) continue;

                var desc = new ComponentFieldDescriptor
                {
                    FieldName = field.Name,
                    Label = fieldAttr.Label ?? field.Name,
                    TypeName = field.FieldType.FullName ?? field.FieldType.Name,
                    Kind = InferFieldKind(field.FieldType),
                };

                var rangeAttr = field.GetCustomAttribute<RangeAttribute>();
                if (rangeAttr != null) { desc.Min = rangeAttr.Min; desc.Max = rangeAttr.Max; desc.Kind = FieldKind.Slider; }

                var minAttr = field.GetCustomAttribute<MinAttribute>();
                if (minAttr != null) desc.Min = minAttr.Value;

                var maxAttr = field.GetCustomAttribute<MaxAttribute>();
                if (maxAttr != null) desc.Max = maxAttr.Value;

                var sliderAttr = field.GetCustomAttribute<SliderAttribute>();
                if (sliderAttr != null) { desc.Kind = FieldKind.Slider; desc.Step = sliderAttr.Step; }

                var colorAttr = field.GetCustomAttribute<ColorAttribute>();
                if (colorAttr != null) { desc.IsColor = true; desc.Kind = FieldKind.Color; }

                var tooltipAttr = field.GetCustomAttribute<TooltipAttribute>();
                if (tooltipAttr != null) desc.Tooltip = tooltipAttr.Text;

                fields.Add(desc);
            }

            if (fields.Count > 0)
            {
                result.Add(new InspectableComponentDescriptor
                {
                    TypeName = type.FullName ?? type.Name,
                    DisplayName = type.Name,
                    Fields = fields
                });
            }
        }

        return result;
    }

    private static FieldKind InferFieldKind(Type type)
    {
        if (type == typeof(bool)) return FieldKind.Bool;
        if (type == typeof(int) || type == typeof(uint) || type == typeof(long)) return FieldKind.Int;
        if (type == typeof(float) || type == typeof(double)) return FieldKind.Float;
        if (type == typeof(string)) return FieldKind.Text;
        if (type.IsEnum) return FieldKind.Enum;
        var name = type.Name;
        if (name.Contains("Vector2")) return FieldKind.Vector2;
        if (name.Contains("Vector3")) return FieldKind.Vector3;
        if (name.Contains("Vector4")) return FieldKind.Vector4;
        return FieldKind.Text;
    }

    // ── Assembly Load Context ───────────────────────────────────────────

    private void UnloadCurrent()
    {
        if (_currentContext == null) return;
        _currentContext.Unload();
        _currentContext = null;
    }

    private void AddDefaultReferences()
    {
        // Core runtime assemblies
        var trustedPlatformAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string;
        if (trustedPlatformAssemblies != null)
        {
            foreach (var path in trustedPlatformAssemblies.Split(Path.PathSeparator))
            {
                var fileName = Path.GetFileName(path);
                // Include essential runtime assemblies
                if (fileName.StartsWith("System.") || fileName.StartsWith("Microsoft.") ||
                    fileName == "mscorlib.dll" || fileName == "netstandard.dll")
                {
                    try { _references.Add(MetadataReference.CreateFromFile(path)); }
                    catch { /* skip inaccessible assemblies */ }
                }
            }
        }

        // Add Editor.Shell itself (so scripts can reference the builder API)
        AddReference(typeof(ShellRegistry).Assembly);
    }

    private sealed class ScriptLoadContext(string name) : AssemblyLoadContext(name, isCollectible: true)
    {
        protected override Assembly? Load(AssemblyName assemblyName)
        {
            // Defer to the default context for all shared assemblies
            return null;
        }
    }
}

// ── Compilation Result ──────────────────────────────────────────────────

/// <summary>Result of a script compilation attempt.</summary>
public sealed class ScriptCompilationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] Files { get; set; } = [];
    public List<ScriptError> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

/// <summary>A single compilation error with location info.</summary>
public sealed class ScriptError
{
    public string FileName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}
