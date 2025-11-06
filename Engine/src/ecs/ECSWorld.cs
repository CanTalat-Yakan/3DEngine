
amespace Engine;

public sealed class ECSWorld
{
    private int _nextEntity = 1;
    private readonly Dictionary<System.Type, IDictionary<int, object>> _components = new();

    public int Spawn() => _nextEntity++;

    public void Despawn(int entity)
    {
        foreach (var kv in _components.Values)
            kv.Remove(entity);
    }

    public void Add<T>(int entity, T component)
    {
        if (!_components.TryGetValue(typeof(T), out var map))
        {
            map = new Dictionary<int, object>();
            _components[typeof(T)] = map;
        }
        map[entity] = component!;
    }

    public bool TryGet<T>(int entity, out T? component)
    {
        component = default;
        if (_components.TryGetValue(typeof(T), out var map) && map.TryGetValue(entity, out var obj))
        {
            component = (T)obj;
            return true;
        }
        return false;
    }

    public IEnumerable<(int Entity, T Component)> Query<T>()
    {
        if (_components.TryGetValue(typeof(T), out var map))
        {
            foreach (var kv in map)
                yield return (kv.Key, (T)kv.Value);
        }
    }
}

