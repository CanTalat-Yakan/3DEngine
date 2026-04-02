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
public sealed partial class ShellCompiler : IDisposable
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
    public event Action<ShellCompilationResult>? CompilationCompleted;

    public ShellCompiler(ShellRegistry registry)
    {
        _registry = registry ?? throw new ArgumentNullException(nameof(registry));
        AddDefaultReferences();
    }
}

