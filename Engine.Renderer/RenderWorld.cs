using System.Collections.Concurrent;

namespace Engine;

/// <summary>
/// Render-thread resource container, analogous to <see cref="World"/> but for GPU-side data.
/// Stores render-specific resources (cameras, draw lists, surface info) using a thread-safe dictionary.
/// </summary>
/// <seealso cref="Renderer"/>
/// <seealso cref="World"/>
public sealed class RenderWorld
{
    private readonly ConcurrentDictionary<Type, object> _resources = new();

    /// <summary>Returns <c>true</c> if a resource of type <typeparamref name="T"/> is stored.</summary>
    /// <typeparam name="T">The resource type to check for.</typeparam>
    /// <returns><c>true</c> if present; otherwise <c>false</c>.</returns>
    public bool Contains<T>() where T : notnull => _resources.ContainsKey(typeof(T));

    /// <summary>Gets the resource of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <returns>The resource instance.</returns>
    /// <exception cref="KeyNotFoundException">Thrown if the resource is not present.</exception>
    public T Get<T>() where T : notnull => (T)_resources[typeof(T)];

    /// <summary>Tries to get the resource of type <typeparamref name="T"/>, returning <c>null</c> if missing.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <returns>The resource instance, or <c>default</c> if not present.</returns>
    public T? TryGet<T>() where T : notnull => _resources.TryGetValue(typeof(T), out var obj) ? (T?)obj : default;

    /// <summary>Sets (inserts or overwrites) a resource of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <param name="value">The resource instance to store.</param>
    public void Set<T>(T value) where T : notnull => _resources[typeof(T)] = value!;

    /// <summary>Removes the resource of type <typeparamref name="T"/> if present.</summary>
    /// <typeparam name="T">The resource type.</typeparam>
    /// <returns><c>true</c> if the resource was removed; <c>false</c> if it was not present.</returns>
    public bool Remove<T>() where T : notnull => _resources.TryRemove(typeof(T), out _);
}