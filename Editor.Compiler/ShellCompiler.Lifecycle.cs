namespace Editor.Shell;

public sealed partial class ShellCompiler
{
    /// <summary>
    /// Performs an initial compilation and starts file watchers.
    /// Call once after configuration.
    /// </summary>
    /// <returns>The result of the initial compilation.</returns>
    public ShellCompilationResult Start()
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
    /// <returns>The result of the recompilation.</returns>
    public ShellCompilationResult Recompile() => CompileAndLoad();

    /// <summary>Disposes the debounce timer, file watchers, and the current script load context.</summary>
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
}
