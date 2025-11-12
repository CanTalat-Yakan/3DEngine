using System.Collections.Concurrent;

namespace Engine;

public sealed class RenderWorld
{
    private readonly ConcurrentDictionary<Type, object> _resources = new();
    public bool Contains<T>() where T : notnull => _resources.ContainsKey(typeof(T));
    public T Get<T>() where T : notnull => (T)_resources[typeof(T)];
    public T? TryGet<T>() where T : notnull => _resources.TryGetValue(typeof(T), out var obj) ? (T?)obj : default;
    public void Set<T>(T value) where T : notnull => _resources[typeof(T)] = value!;
    public bool Remove<T>() where T : notnull => _resources.TryRemove(typeof(T), out _);
}