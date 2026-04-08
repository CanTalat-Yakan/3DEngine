using System.Collections.Concurrent;

namespace Engine;

/// <summary>
/// Minimal ECS-like world focusing on resource storage.
/// Resources are keyed by their concrete <see cref="Type"/>; each type may have at most one instance.
/// </summary>
/// <remarks>
/// <para>
/// Thread-safe for concurrent reads and writes (backed by <see cref="ConcurrentDictionary{TKey,TValue}"/>).
/// Implements <see cref="IDisposable"/> to clean up any resources that themselves implement
/// <see cref="IDisposable"/>.
/// </para>
/// <para>
/// This is the central data container shared across all systems. Use <see cref="InsertResource{T}"/>
/// to store singleton-style resources and <see cref="Resource{T}"/> to retrieve them.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// var world = new World();
/// world.InsertResource(new GameState { Score = 0 });
///
/// // Retrieve a required resource (throws if missing)
/// var state = world.Resource&lt;GameState&gt;();
///
/// // Safely check and retrieve
/// if (world.TryGetResource&lt;GameState&gt;(out var s))
///     Console.WriteLine(s.Score);
/// </code>
/// </example>
/// <seealso cref="App"/>
/// <seealso cref="Events{T}"/>
public sealed class World : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.World");

    private readonly ConcurrentDictionary<Type, object> _resources = new();

    // ── Query ──────────────────────────────────────────────────────────

    /// <summary>Number of resources currently stored in this world.</summary>
    public int ResourceCount => _resources.Count;

    /// <summary>Checks whether a resource of type <typeparamref name="T"/> exists in this world.</summary>
    /// <typeparam name="T">The resource type to look for.</typeparam>
    /// <returns><c>true</c> if a resource of type <typeparamref name="T"/> is present; otherwise <c>false</c>.</returns>
    public bool ContainsResource<T>() where T : notnull => 
        _resources.ContainsKey(typeof(T));

    /// <summary>Gets a required resource of type <typeparamref name="T"/>, or throws if missing.</summary>
    /// <typeparam name="T">The resource type to retrieve.</typeparam>
    /// <returns>The resource instance of type <typeparamref name="T"/>.</returns>
    /// <exception cref="InvalidOperationException">Thrown when no resource of type <typeparamref name="T"/> is found.</exception>
    public T Resource<T>() where T : notnull
    {
        if (_resources.TryGetValue(typeof(T), out var obj) && obj is T typed)
            return typed;
        throw new InvalidOperationException($"Resource of type {typeof(T).Name} not found.");
    }

    /// <summary>Returns the resource of type <typeparamref name="T"/>, or <c>null</c>/<c>default</c> if not present.</summary>
    /// <typeparam name="T">The resource type to retrieve.</typeparam>
    /// <returns>The resource instance, or <c>default</c> if not found.</returns>
    public T? TryResource<T>() where T : notnull =>
        _resources.TryGetValue(typeof(T), out var obj) ? (T?)obj : default;

    /// <summary>Tries to get a resource of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The resource type to retrieve.</typeparam>
    /// <param name="value">When this method returns <c>true</c>, contains the resource instance; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if the resource was found; otherwise <c>false</c>.</returns>
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

    /// <summary>Returns a snapshot of all resource types currently stored in this world.</summary>
    /// <returns>A read-only collection of <see cref="Type"/> objects for each stored resource.</returns>
    public IReadOnlyCollection<Type> ResourceTypes => _resources.Keys.ToArray();

    // ── Mutation ───────────────────────────────────────────────────────

    /// <summary>Inserts or replaces a resource of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The resource type. Keyed by concrete type - at most one instance per type.</typeparam>
    /// <param name="value">The resource instance to store. Replaces any existing resource of the same type.</param>
    public void InsertResource<T>(T value) where T : notnull => 
        _resources[typeof(T)] = value;

    /// <summary>
    /// Returns the existing resource of type <typeparamref name="T"/>, or inserts <paramref name="value"/> and returns it.
    /// Atomic - safe for concurrent callers.
    /// </summary>
    /// <typeparam name="T">The resource type to retrieve or insert.</typeparam>
    /// <param name="value">The fallback value to insert if the resource does not exist.</param>
    /// <returns>The existing or newly inserted resource instance.</returns>
    public T GetOrInsertResource<T>(T value) where T : notnull => 
        (T)_resources.GetOrAdd(typeof(T), value);

    /// <summary>
    /// Returns the existing resource of type <typeparamref name="T"/>, or creates one via <paramref name="factory"/>,
    /// inserts it, and returns it. Atomic - safe for concurrent callers.
    /// The factory is only invoked when the resource is missing.
    /// </summary>
    /// <typeparam name="T">The resource type to retrieve or create.</typeparam>
    /// <param name="factory">A delegate invoked to create the resource when it does not exist.</param>
    /// <returns>The existing or newly created resource instance.</returns>
    public T GetOrInsertResource<T>(Func<T> factory) where T : notnull => 
        (T)_resources.GetOrAdd(typeof(T), _ => factory());

    /// <summary>
    /// Returns the existing resource of type <typeparamref name="T"/>, or creates a default instance via <c>new T()</c>,
    /// inserts it, and returns it. Convenience overload for resources with parameterless constructors.
    /// </summary>
    /// <typeparam name="T">The resource type. Must have a public parameterless constructor.</typeparam>
    /// <returns>The existing or newly created resource instance.</returns>
    public T InitResource<T>() where T : notnull, new() => 
        (T)_resources.GetOrAdd(typeof(T), _ => new T());

    /// <summary>Removes the resource of type <typeparamref name="T"/> if present.</summary>
    /// <typeparam name="T">The resource type to remove.</typeparam>
    /// <returns><c>true</c> if a resource was removed; <c>false</c> if no resource of that type existed.</returns>
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

    /// <summary>
    /// Disposes all <see cref="IDisposable"/> resources and clears the world.
    /// Equivalent to calling <see cref="Clear"/>.
    /// </summary>
    public void Dispose() => Clear();
}