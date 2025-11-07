namespace Engine;

/// <summary>Lightweight ECS storage managing entities, components, and per-frame change tracking.</summary>
public sealed class EcsWorld
{
    private int _nextEntity = 1;
    private readonly Dictionary<Type, IComponentStore> _stores = new();
    private int _currentTick = 0;

    /// <summary>Spawns a new entity id.</summary>
    public int Spawn() => _nextEntity++;

    /// <summary>Removes an entity and all of its components, disposing IDisposable components.</summary>
    public void Despawn(int entity)
    {
        foreach (var store in _stores.Values)
        {
            if (store.TryRemove(entity, out var obj) && obj is IDisposable d)
            {
                try { d.Dispose(); } catch { /* swallow disposal exceptions */ }
            }
        }
    }

    /// <summary>Adds a component to an entity (overwrites if existing).</summary>
    public void Add<T>(int entity, T component) => GetStore<T>().Add(entity, component);

    /// <summary>Updates an existing component (or adds if missing) and marks it changed for this frame.</summary>
    public void Update<T>(int entity, T component) => GetStore<T>().Update(entity, component, _currentTick);

    /// <summary>Updates an existing component via transformer and marks it changed; no-op if missing.</summary>
    public void Mutate<T>(int entity, Func<T, T> mutate)
    {
        var store = GetStore<T>(create: false);
        if (store is null) return;
        if (store.TryGet(entity, out var value))
        {
            var next = mutate(value);
            store.Update(entity, next, _currentTick);
        }
    }

    /// <summary>Checks if entity has component T.</summary>
    public bool Has<T>(int entity)
    {
        var store = GetStore<T>(create: false);
        return store != null && store.Has(entity);
    }

    /// <summary>Checks if component T on entity was updated this frame.</summary>
    public bool Changed<T>(int entity)
    {
        var store = GetStore<T>(create: false);
        return store != null && store.ChangedThisFrame(entity, _currentTick);
    }

    /// <summary>Advances frame tick; used for per-frame change tracking.</summary>
    public void BeginFrame()
    {
        _currentTick++;
        // Extremely long-running sessions: handle wrap-around defensively
        if (_currentTick == int.MaxValue)
        {
            _currentTick = 1;
            foreach (var s in _stores.Values) s.ClearChangedTicks();
        }
    }

    /// <summary>Attempts to get component T for entity.</summary>
    public bool TryGet<T>(int entity, out T? component)
    {
        var store = GetStore<T>(create: false);
        if (store != null && store.TryGet(entity, out var value))
        {
            component = value;
            return true;
        }
        component = default;
        return false;
    }

    /// <summary>Enumerates all entities with component T.</summary>
    public IEnumerable<(int Entity, T Component)> Query<T>()
    {
        var store = GetStore<T>(create: false);
        if (store == null) yield break;
        foreach (var (entity, comp) in store.Enumerate())
            yield return (entity, comp);
    }

    /// <summary>Enumerates entities having both components T1 and T2.</summary>
    public IEnumerable<(int Entity, T1 C1, T2 C2)> Query<T1, T2>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        if (s1 == null || s2 == null) yield break;

        if (s1.Count <= s2.Count)
        {
            foreach (var (entity, c1) in s1.Enumerate())
                if (s2.TryGet(entity, out var c2))
                    yield return (entity, c1, c2);
        }
        else
        {
            foreach (var (entity, c2) in s2.Enumerate())
                if (s1.TryGet(entity, out var c1))
                    yield return (entity, c1, c2);
        }
    }

    /// <summary>Enumerates entities having components T1, T2 and T3.</summary>
    public IEnumerable<(int Entity, T1 C1, T2 C2, T3 C3)> Query<T1, T2, T3>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        var s3 = GetStore<T3>(create: false);
        if (s1 == null || s2 == null || s3 == null) yield break;

        int smallest = 1;
        var c1 = s1.Count; var c2 = s2.Count; var c3 = s3.Count;
        if (c2 <= c1 && c2 <= c3) smallest = 2;
        else if (c3 < c1 && c3 < c2) smallest = 3;

        if (smallest == 1)
        {
            foreach (var (e, comp1) in s1.Enumerate())
            {
                if (!s2.TryGet(e, out var comp2) || !s3.TryGet(e, out var comp3)) continue;
                yield return (e, comp1, comp2, comp3);
            }
        }
        else if (smallest == 2)
        {
            foreach (var (e, comp2) in s2.Enumerate())
            {
                if (!s1.TryGet(e, out var comp1) || !s3.TryGet(e, out var comp3)) continue;
                yield return (e, comp1, comp2, comp3);
            }
        }
        else
        {
            foreach (var (e, comp3) in s3.Enumerate())
            {
                if (!s1.TryGet(e, out var comp1) || !s2.TryGet(e, out var comp2)) continue;
                yield return (e, comp1, comp2, comp3);
            }
        }
    }

    /// <summary>Enumerates components of type T passing the predicate.</summary>
    public IEnumerable<(int Entity, T Component)> QueryWhere<T>(Func<T, bool> predicate)
    {
        foreach (var (entity, comp) in Query<T>())
            if (predicate(comp))
                yield return (entity, comp);
    }

    /// <summary>Transforms each component of type T via provided function and marks it changed.</summary>
    public void TransformEach<T>(Func<int, T, T> transform)
    {
        var store = GetStore<T>(create: false);
        if (store == null) return;
        foreach (var (entity, comp) in store.Enumerate())
        {
            var newVal = transform(entity, comp);
            store.Update(entity, newVal, _currentTick);
        }
    }

    // Internal typed store plumbing
    private ComponentStore<T> GetStore<T>(bool create = true)
    {
        var type = typeof(T);
        if (_stores.TryGetValue(type, out var s))
            return (ComponentStore<T>)s;
        if (!create) return null!;
        var created = new ComponentStore<T>();
        _stores[type] = created;
        return created;
    }

    private interface IComponentStore
    {
        int Count { get; }
        bool TryRemove(int entity, out object? previous);
        void ClearChangedTicks();
    }

    private sealed class ComponentStore<T> : IComponentStore
    {
        private readonly Dictionary<int, T> _values = new();
        private readonly Dictionary<int, int> _changedTick = new();

        public int Count => _values.Count;

        public void Add(int entity, T component) => _values[entity] = component!;

        public void Update(int entity, T component, int currentTick)
        {
            _values[entity] = component!;
            _changedTick[entity] = currentTick;
        }

        public bool Has(int entity) => _values.ContainsKey(entity);

        public bool TryGet(int entity, out T value) => _values.TryGetValue(entity, out value!);

        public bool ChangedThisFrame(int entity, int currentTick)
        {
            return _changedTick.TryGetValue(entity, out var t) && t == currentTick;
        }

        public IEnumerable<(int Entity, T Component)> Enumerate()
        {
            // Snapshot keys to allow safe updates during iteration
            var keys = new int[_values.Count];
            _values.Keys.CopyTo(keys, 0);
            foreach (var e in keys)
            {
                if (_values.TryGetValue(e, out var v))
                    yield return (e, v);
            }
        }

        public bool TryRemove(int entity, out object? previous)
        {
            if (_values.TryGetValue(entity, out var v))
            {
                previous = v!; // box if struct; only on removal
                _values.Remove(entity);
                _changedTick.Remove(entity);
                return true;
            }
            previous = null;
            return false;
        }

        public void ClearChangedTicks() => _changedTick.Clear();
    }
}
