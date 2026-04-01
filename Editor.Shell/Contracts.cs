namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  SignalR Contracts — shared between engine and Blazor server.
//  Pure interfaces: no ASP.NET dependency. Editor.Server implements the hub;
//  the engine pushes state via IEditorClient callbacks.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Methods the Blazor client can call on the server hub.
/// </summary>
public interface IEditorHub
{
    /// <summary>Select an entity by its ECS entity id.</summary>
    Task SelectEntity(int entityId);

    /// <summary>Deselect the current entity.</summary>
    Task DeselectEntity();

    /// <summary>Update a single component field value on the selected entity.</summary>
    Task UpdateComponentField(int entityId, string componentType, string fieldName, string jsonValue);

    /// <summary>Execute a named command (e.g., "NewScene", "Save", "Undo").</summary>
    Task ExecuteCommand(string commandId, string? jsonArgs = null);

    /// <summary>Request the current shell descriptor version (for reconnect sync).</summary>
    Task<int> GetShellVersion();
}

/// <summary>
/// Callbacks the server pushes to connected Blazor clients.
/// </summary>
public interface IEditorClient
{
    /// <summary>Entity selection changed in the engine.</summary>
    Task OnEntitySelected(int entityId);

    /// <summary>Entity was deselected.</summary>
    Task OnEntityDeselected();

    /// <summary>Component data changed for the selected entity.</summary>
    Task OnComponentsChanged(int entityId, string jsonComponents);

    /// <summary>A console log message from the engine.</summary>
    Task OnConsoleLog(string level, string category, string message);

    /// <summary>Engine statistics update (FPS, frame time, etc.).</summary>
    Task OnStatsUpdated(string jsonStats);

    /// <summary>The shell descriptor was rebuilt (hot-reload). Client should re-fetch and re-render.</summary>
    Task OnShellChanged(int newVersion);

    /// <summary>A script compilation error occurred.</summary>
    Task OnScriptError(string fileName, string message, int line, int column);

    /// <summary>Script compilation succeeded.</summary>
    Task OnScriptCompiled(string[] fileNames);
}

/// <summary>Snapshot of engine stats pushed to the editor.</summary>
public sealed class EngineStats
{
    public double Fps { get; set; }
    public double FrameTimeMs { get; set; }
    public ulong FrameCount { get; set; }
    public int EntityCount { get; set; }
    public int SystemCount { get; set; }
    public int ResourceCount { get; set; }
    public Dictionary<string, double> StageDurationsMs { get; set; } = [];
}
