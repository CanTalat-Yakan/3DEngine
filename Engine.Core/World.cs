using System.Collections.Concurrent;

namespace Engine;

/// <summary>Minimal ECS-like world focusing on Bevy-style resource storage.</summary>
public sealed class World
{
    private readonly ConcurrentDictionary<Type, object> _resources = new();

    /// <summary>Checks if a resource of type T exists.</summary>
    public bool ContainsResource<T>() where T : notnull => _resources.ContainsKey(typeof(T));

    /// <summary>Gets a required resource of type T or throws if missing.</summary>
    public T Resource<T>() where T : notnull
    {
        if (_resources.TryGetValue(typeof(T), out var obj) && obj is T typed)
            return typed;
        throw new InvalidOperationException($"Resource of type {typeof(T).Name} not found.");
    }

    /// <summary>Returns the resource of type T or null/default if not present.</summary>
    public T? TryResource<T>() where T : notnull
        => _resources.TryGetValue(typeof(T), out var obj) ? (T?)obj : default;

    /// <summary>Inserts or replaces a resource of type T.</summary>
    public void InsertResource<T>(T value) where T : notnull
        => _resources[typeof(T)] = value!;

    /// <summary>Removes the resource of type T if present.</summary>
    public bool RemoveResource<T>() where T : notnull
        => _resources.TryRemove(typeof(T), out _);
}
