namespace Editor.Shell;

/// <summary>Result of a shell script compilation attempt.</summary>
public sealed class ShellCompilationResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string[] Files { get; set; } = [];
    public List<ShellCompilationError> Errors { get; set; } = [];
    public List<string> Warnings { get; set; } = [];
}

/// <summary>A single compilation error with location info.</summary>
public sealed class ShellCompilationError
{
    public string FileName { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int Line { get; set; }
    public int Column { get; set; }
}

