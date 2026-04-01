using Editor.Shell;
using Microsoft.AspNetCore.SignalR;

namespace Editor.Server.Hubs;

/// <summary>
/// SignalR hub bridging the Blazor editor UI and the engine runtime.
/// Implements <see cref="IEditorHub"/> (client-to-server calls) and pushes
/// engine state changes via <see cref="IEditorClient"/> callbacks.
/// </summary>
public sealed class EditorHub : Hub<IEditorClient>, IEditorHub
{
    private readonly ShellRegistry _registry;
    private readonly EditorState _state;

    public EditorHub(ShellRegistry registry, EditorState state)
    {
        _registry = registry;
        _state = state;
    }

    public Task SelectEntity(int entityId)
    {
        _state.SelectedEntityId = entityId;
        return Clients.Others.OnEntitySelected(entityId);
    }

    public Task DeselectEntity()
    {
        _state.SelectedEntityId = null;
        return Clients.Others.OnEntityDeselected();
    }

    public Task UpdateComponentField(int entityId, string componentType, string fieldName, string jsonValue)
    {
        _state.PendingFieldEdits.Enqueue(new FieldEdit(entityId, componentType, fieldName, jsonValue));
        return Task.CompletedTask;
    }

    public Task ExecuteCommand(string commandId, string? jsonArgs = null)
    {
        _state.PendingCommands.Enqueue(new EditorCommand(commandId, jsonArgs));
        return Task.CompletedTask;
    }

    public Task<int> GetShellVersion() => Task.FromResult(_registry.Version);

    public override Task OnConnectedAsync()
    {
        // Push current shell state to the newly connected client
        return Clients.Caller.OnShellChanged(_registry.Version);
    }
}

/// <summary>
/// Shared mutable state between the SignalR hub and the engine bridge.
/// Thread-safe queues for pending edits and commands.
/// </summary>
public sealed class EditorState
{
    public int? SelectedEntityId { get; set; }
    public System.Collections.Concurrent.ConcurrentQueue<FieldEdit> PendingFieldEdits { get; } = new();
    public System.Collections.Concurrent.ConcurrentQueue<EditorCommand> PendingCommands { get; } = new();
}

public sealed record FieldEdit(int EntityId, string ComponentType, string FieldName, string JsonValue);
public sealed record EditorCommand(string CommandId, string? JsonArgs);
