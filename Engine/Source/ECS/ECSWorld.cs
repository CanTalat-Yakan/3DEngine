namespace Engine;

public sealed class ECSWorld
{
    private int _nextEntity = 1;
    private readonly Dictionary<Type, Dictionary<int, object>> _components = new();
    private readonly HashSet<(Type Type, int Entity)> _changed = new();

    public int Spawn() => _nextEntity++;

    public void Despawn(int entity)
    {
        foreach (var map in _components.Values)
            map.Remove(entity);
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

    public void Update<T>(int entity, T component)
    {
        if (_components.TryGetValue(typeof(T), out var map))
            map[entity] = component!;
        else
        {
            map = new Dictionary<int, object>();
            _components[typeof(T)] = map;
            map[entity] = component!;
        }
        _changed.Add((typeof(T), entity));
    }

    public bool Has<T>(int entity)
    {
        return _components.TryGetValue(typeof(T), out var map) && map.ContainsKey(entity);
    }

    public bool Changed<T>(int entity)
    {
        return _changed.Contains((typeof(T), entity));
    }

    public void BeginFrame()
    {
        _changed.Clear();
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
            foreach (var kv in map.ToArray())
                yield return (kv.Key, (T)kv.Value);
        }
    }

    public IEnumerable<(int Entity, T1 C1, T2 C2)> Query<T1, T2>()
    {
        if (!_components.TryGetValue(typeof(T1), out var map1) || !_components.TryGetValue(typeof(T2), out var map2))
            yield break;

        var (smallMap, otherMap, otherIsFirst) = map1.Count <= map2.Count
            ? (map1, map2, true)
            : (map2, map1, false);

        foreach (var (entity, objSmall) in smallMap.ToArray())
        {
            if (!otherMap.TryGetValue(entity, out var objOther)) continue;
            if (otherIsFirst)
                yield return (entity, (T1)objOther, (T2)objSmall);
            else
                yield return (entity, (T1)objSmall, (T2)objOther);
        }
    }

    public IEnumerable<(int Entity, T1 C1, T2 C2, T3 C3)> Query<T1, T2, T3>()
    {
        if (!_components.TryGetValue(typeof(T1), out var map1) ||
            !_components.TryGetValue(typeof(T2), out var map2) ||
            !_components.TryGetValue(typeof(T3), out var map3))
            yield break;

        // pick smallest map for iteration
        var maps = new List<(Type t, Dictionary<int, object> m)> {
            (typeof(T1), map1), (typeof(T2), map2), (typeof(T3), map3)
        };
        maps.Sort((a,b) => a.m.Count.CompareTo(b.m.Count));
        var small = maps[0];
        foreach (var (entity, objSmall) in small.m.ToArray())
        {
            if (!map1.TryGetValue(entity, out var o1) || !map2.TryGetValue(entity, out var o2) || !map3.TryGetValue(entity, out var o3))
                continue;
            yield return (entity, (T1)o1, (T2)o2, (T3)o3);
        }
    }

    public IEnumerable<(int Entity, T Component)> QueryWhere<T>(Func<T, bool> predicate)
    {
        foreach (var (entity, comp) in Query<T>())
            if (predicate(comp))
                yield return (entity, comp);
    }

    public void TransformEach<T>(Func<int, T, T> transform)
    {
        if (!_components.TryGetValue(typeof(T), out var map)) return;
        foreach (var (entity, obj) in map.ToArray())
        {
            var newVal = transform(entity, (T)obj);
            map[entity] = newVal!;
        }
    }
}
