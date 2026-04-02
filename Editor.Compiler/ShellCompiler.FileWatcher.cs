namespace Editor.Shell;

public sealed partial class ShellCompiler
{
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
}

