using System.Collections.Concurrent;

namespace Engine;

/// <summary>
/// Minimal ECS-like world focusing on resource storage (Bevy-style resources).
/// </summary>
public sealed class World
{
    private readonly ConcurrentDictionary<Type, object> _resources = new();

    public bool ContainsResource<T>() where T : notnull => _resources.ContainsKey(typeof(T));

    public T Resource<T>() where T : notnull
    {
        if (_resources.TryGetValue(typeof(T), out var obj) && obj is T typed)
            return typed;
        throw new InvalidOperationException($"Resource of type {typeof(T).Name} not found.");
    }

    public T? TryResource<T>() where T : notnull
        => _resources.TryGetValue(typeof(T), out var obj) ? (T?)obj : default;

    public void InsertResource<T>(T value) where T : notnull
        => _resources[typeof(T)] = value!;

    public bool RemoveResource<T>() where T : notnull
        => _resources.TryRemove(typeof(T), out _);
}


