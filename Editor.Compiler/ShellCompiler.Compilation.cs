using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Editor.Shell;

public sealed partial class ShellCompiler
{
    /// <summary>
    /// Performs a full compilation cycle: collect files → parse → compile → load → discover builders → update registry.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The entire operation runs under <c>_compileLock</c> to prevent concurrent compilations.
    /// On success, the previous <c>AssemblyLoadContext</c> is unloaded and the new
    /// descriptor is pushed to the <see cref="ShellRegistry"/>.
    /// </para>
    /// <para>If no script files are found, an empty <see cref="ShellDescriptor"/> is pushed.</para>
    /// </remarks>
    /// <returns>A <see cref="ShellCompilationResult"/> describing success, errors, and warnings.</returns>
    private ShellCompilationResult CompileAndLoad()
    {
        lock (_compileLock)
        {
            var result = new ShellCompilationResult();

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
                _registry.Update(new ShellDescriptor());
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
                    result.Errors.Add(new ShellCompilationError
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
                        result.Errors.Add(new ShellCompilationError
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

            // Push to registry
            _registry.Update(shellDescriptor);

            result.Success = true;
            result.Message = $"Compiled {files.Count} file(s) successfully (gen {gen}).";
            return result;
        }
    }

    /// <summary>
    /// Discovers types annotated with <see cref="EditorShellAttribute"/> that implement
    /// <see cref="IEditorShellBuilder"/>, instantiates them, and executes their
    /// <see cref="IEditorShellBuilder.Build"/> method in priority order.
    /// </summary>
    /// <param name="assembly">The compiled script assembly to scan.</param>
    /// <param name="result">Compilation result to append warnings to on instantiation/build failures.</param>
    /// <returns>The assembled <see cref="ShellDescriptor"/>.</returns>
    private static ShellDescriptor DiscoverAndBuildShell(Assembly assembly, ShellCompilationResult result)
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
}

