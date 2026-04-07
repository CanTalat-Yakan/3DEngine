using System.Collections.Concurrent;

namespace Engine;

/// <summary>
/// Minimal ECS-like world focusing on Bevy-style resource storage.
/// Resources are keyed by their concrete <see cref="Type"/>; each type may have at most one instance.
/// <para>
/// Thread-safe for concurrent reads and writes (backed by <see cref="ConcurrentDictionary{TKey,TValue}"/>).
/// Implements <see cref="IDisposable"/> to clean up any resources that themselves implement
/// <see cref="IDisposable"/>.
/// </para>
/// </summary>
public sealed class World : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.World");

    private readonly ConcurrentDictionary<Type, object> _resources = new();

    // ── Query ──────────────────────────────────────────────────────────

    /// <summary>Number of resources currently stored.</summary>
    public int ResourceCount => _resources.Count;

    /// <summary>Checks if a resource of type T exists.</summary>
    public bool ContainsResource<T>() where T : notnull => 
        _resources.ContainsKey(typeof(T));

    /// <summary>Gets a required resource of type T or throws if missing.</summary>
    /// <exception cref="InvalidOperationException">Thrown when no resource of type T is found.</exception>
    public T Resource<T>() where T : notnull
    {
        if (_resources.TryGetValue(typeof(T), out var obj) && obj is T typed)
            return typed;
        throw new InvalidOperationException($"Resource of type {typeof(T).Name} not found.");
    }

    /// <summary>Returns the resource of type T or null/default if not present.</summary>
    public T? TryResource<T>() where T : notnull =>
        _resources.TryGetValue(typeof(T), out var obj) ? (T?)obj : default;

    /// <summary>Tries to get a resource of type T. Returns true and sets <paramref name="value"/> if found.</summary>
    public bool TryGetResource<T>(out T value) where T : notnull
    {
        if (_resources.TryGetValue(typeof(T), out var obj) && obj is T typed)
        {
            value = typed;
            return true;
        }
        value = default!;
        return false;
    }

    /// <summary>Returns a snapshot of all resource types currently stored.</summary>
    public IReadOnlyCollection<Type> ResourceTypes => _resources.Keys.ToArray();

    // ── Mutation ───────────────────────────────────────────────────────

    /// <summary>Inserts or replaces a resource of type T.</summary>
    public void InsertResource<T>(T value) where T : notnull => 
        _resources[typeof(T)] = value;

    /// <summary>
    /// Returns the existing resource of type T, or inserts <paramref name="value"/> and returns it.
    /// Atomic - safe for concurrent callers.
    /// </summary>
    public T GetOrInsertResource<T>(T value) where T : notnull => 
        (T)_resources.GetOrAdd(typeof(T), value);

    /// <summary>
    /// Returns the existing resource of type T, or creates one via <paramref name="factory"/>,
    /// inserts it, and returns it. Atomic - safe for concurrent callers.
    /// The factory is only invoked when the resource is missing.
    /// </summary>
    public T GetOrInsertResource<T>(Func<T> factory) where T : notnull => 
        (T)_resources.GetOrAdd(typeof(T), _ => factory());

    /// <summary>
    /// Returns the existing resource of type T, or creates a default instance via <c>new T()</c>,
    /// inserts it, and returns it. Convenience overload for resources with parameterless constructors.
    /// </summary>
    public T InitResource<T>() where T : notnull, new() => 
        (T)_resources.GetOrAdd(typeof(T), _ => new T());

    /// <summary>Removes the resource of type T if present. Returns true if a resource was removed.</summary>
    public bool RemoveResource<T>() where T : notnull => 
        _resources.TryRemove(typeof(T), out _);

    // ── Lifecycle ──────────────────────────────────────────────────────

    /// <summary>
    /// Removes all resources, disposing any that implement <see cref="IDisposable"/>.
    /// Exceptions during individual dispose calls are logged and swallowed so that
    /// remaining resources are still cleaned up.
    /// </summary>
    public void Clear()
    {
        foreach (var kv in _resources)
            if (kv.Value is IDisposable d)
            {
                try
                {
                    d.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.Error($"Error disposing resource {kv.Key.Name}", ex);
                }
            }

        _resources.Clear();
        Logger.Trace("World cleared - all resources removed.");
    }

    /// <summary>Disposes all <see cref="IDisposable"/> resources and clears the world.</summary>
    public void Dispose() => Clear();
}