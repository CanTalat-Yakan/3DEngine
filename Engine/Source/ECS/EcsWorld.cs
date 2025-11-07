namespace Engine;

/// <summary>Lightweight ECS storage managing entities, components, and per-frame change tracking.</summary>
public sealed class EcsWorld
{
    private int _nextEntity = 1;
    private readonly Dictionary<Type, Dictionary<int, object>> _components = new();
    private readonly HashSet<(Type Type, int Entity)> _changed = new();

    /// <summary>Spawns a new entity id.</summary>
    public int Spawn() => _nextEntity++;

    /// <summary>Removes an entity and all of its components, disposing IDisposable components.</summary>
    public void Despawn(int entity)
    {
        foreach (var kv in _components.ToArray())
        {
            var type = kv.Key;
            var map = kv.Value;
            if (!map.TryGetValue(entity, out var obj))
            {
                _changed.Remove((type, entity));
                continue;
            }

            try
            {
                if (obj is IDisposable d)
                    d.Dispose();
            }
            catch
            {
                // Swallow disposal exceptions to avoid tearing down the world mid-frame
            }
            finally
            {
                map.Remove(entity);
                _changed.Remove((type, entity));
            }
        }
    }

    /// <summary>Adds a component to an entity (overwrites if existing).</summary>
    public void Add<T>(int entity, T component)
    {
        if (!_components.TryGetValue(typeof(T), out var map))
        {
            map = new Dictionary<int, object>();
            _components[typeof(T)] = map;
        }
        map[entity] = component!;
    }

    /// <summary>Updates an existing component (or adds if missing) and marks it changed for this frame.</summary>
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

    /// <summary>Checks if entity has component T.</summary>
    public bool Has<T>(int entity)
    {
        return _components.TryGetValue(typeof(T), out var map) && map.ContainsKey(entity);
    }

    /// <summary>Checks if component T on entity was updated this frame.</summary>
    public bool Changed<T>(int entity)
    {
        return _changed.Contains((typeof(T), entity));
    }

    /// <summary>Clears changed markers at frame start.</summary>
    public void BeginFrame()
    {
        _changed.Clear();
    }

    /// <summary>Attempts to get component T for entity.</summary>
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

    /// <summary>Enumerates all entities with component T.</summary>
    public IEnumerable<(int Entity, T Component)> Query<T>()
    {
        if (_components.TryGetValue(typeof(T), out var map))
        {
            foreach (var kv in map.ToArray())
                yield return (kv.Key, (T)kv.Value);
        }
    }

    /// <summary>Enumerates entities having both components T1 and T2.</summary>
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

    /// <summary>Enumerates entities having components T1, T2 and T3.</summary>
    public IEnumerable<(int Entity, T1 C1, T2 C2, T3 C3)> Query<T1, T2, T3>()
    {
        if (!_components.TryGetValue(typeof(T1), out var map1) ||
            !_components.TryGetValue(typeof(T2), out var map2) ||
            !_components.TryGetValue(typeof(T3), out var map3))
            yield break;

        var maps = new List<(Type t, Dictionary<int, object> m)> {
            (typeof(T1), map1), (typeof(T2), map2), (typeof(T3), map3)
        };
        maps.Sort((a,b) => a.m.Count.CompareTo(b.m.Count)); // iterate smallest map for perf
        var small = maps[0];
        foreach (var (entity, objSmall) in small.m.ToArray())
        {
            if (!map1.TryGetValue(entity, out var o1) || !map2.TryGetValue(entity, out var o2) || !map3.TryGetValue(entity, out var o3))
                continue;
            yield return (entity, (T1)o1, (T2)o2, (T3)o3);
        }
    }

    /// <summary>Enumerates components of type T passing the predicate.</summary>
    public IEnumerable<(int Entity, T Component)> QueryWhere<T>(Func<T, bool> predicate)
    {
        foreach (var (entity, comp) in Query<T>())
            if (predicate(comp))
                yield return (entity, comp);
    }

    /// <summary>Transforms each component of type T in place via provided function.</summary>
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
