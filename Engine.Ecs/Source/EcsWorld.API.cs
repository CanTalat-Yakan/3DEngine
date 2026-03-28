using System.Runtime.CompilerServices;

namespace Engine;

public sealed partial class EcsWorld
{
    private int _currentTick;

    /// <summary>Spawns a new entity and returns its integer ID.</summary>
    public int Spawn() => _entities.Spawn();

    /// <summary>Returns current generation for an entity id (0 if never used).</summary>
    public int GetGeneration(int entityId) => _entities.GetGeneration(entityId);

    /// <summary>Removes an entity and all of its components, disposing IDisposable components.</summary>
    public void Despawn(int entity)
    {
        foreach (var store in _stores.Values)
            if (store.TryRemove(entity, out var disposable) && disposable is not null)
                try { disposable.Dispose(); } catch { }

        _entities.Despawn(entity);
    }

    /// <summary>Advances frame tick; used for per-frame change tracking (clears changed bitsets).</summary>
    public void BeginFrame()
    {
        _currentTick++;
        foreach (var s in _stores.Values) s.ClearChangedTicks();
    }

    /// <summary>Count of components of type T.</summary>
    public int Count<T>() => GetStore<T>(create: false)?.Count ?? 0;

    /// <summary>Removes component T from entity if present.</summary>
    public bool Remove<T>(int entity)
    {
        var store = GetStore<T>(create: false);
        return store != null && store.Remove(entity);
    }

    /// <summary>Returns span of entity IDs that currently have component T.</summary>
    public ReadOnlySpan<int> EntitiesWith<T>()
    {
        var store = GetStore<T>(create: false);
        return store == null ? ReadOnlySpan<int>.Empty : store.EntitiesSpan();
    }

    /// <summary>Pre-reserves capacity for a component type to reduce resizing.</summary>
    public void Reserve<T>(int componentCapacity, int maxEntityIdHint = 0)
    {
        var store = GetStore<T>();
        store.Reserve(componentCapacity, maxEntityIdHint);
    }

    /// <summary>Adds a component to an entity (overwrites if existing) without marking changed.</summary>
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

    /// <summary>Returns true if entity has component T.</summary>
    public bool Has<T>(int entity)
    {
        var store = GetStore<T>(create: false);
        return store != null && store.Has(entity);
    }

    /// <summary>Returns true if component T on entity was modified this frame.</summary>
    public bool Changed<T>(int entity)
    {
        var store = GetStore<T>(create: false);
        return store != null && store.ChangedThisFrame(entity, _currentTick);
    }

    /// <summary>Attempts to read component T from entity; returns false if not found.</summary>
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

    /// <summary>Returns a ref to component T on entity, or throws if missing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef<T>(int entity)
    {
        var store = GetStore<T>(create: false) ??
                    throw new KeyNotFoundException($"Component {typeof(T).Name} store not found.");
        return ref store.GetRef(entity);
    }

    /// <summary>Applies a transform function to every component of type T.</summary>
    public void TransformEach<T>(Func<int, T, T> transform)
    {
        var store = GetStore<T>(create: false);
        if (store == null) return;
        for (int i = 0; i < store.Count; i++)
        {
            ref var comp = ref store.ComponentRefByDenseIndex(i);
            var newVal = transform(store.EntityByDenseIndex(i), comp);
            comp = newVal;
            store.MarkChangedByDenseIndex(i, _currentTick);
        }
    }

    /// <summary>Parallel version of <see cref="TransformEach{T}"/>.</summary>
    public void ParallelTransformEach<T>(Func<int, T, T> transform)
    {
        var store = GetStore<T>(create: false);
        if (store == null || store.Count == 0) return;
        var count = store.Count;
        var ents = store.EntitiesArray;
        var comps = store.ComponentsArray;
        System.Threading.Tasks.Parallel.For(0, count, i =>
        {
            var newVal = transform(ents[i], comps[i]);
            comps[i] = newVal;
            store.MarkChangedByDenseIndexThreadSafe(i);
        });
    }

    /// <summary>Returns a zero-allocation ref-enumerable over components of type T.</summary>
    public RefEnumerable<T> IterateRef<T>()
    {
        var store = GetStore<T>(create: false);
        if (store == null || store.Count == 0) return RefEnumerable<T>.From(default);
        return RefEnumerable<T>.FromStore(store, markOnIterate: true);
    }

    /// <summary>Returns a zero-allocation ref-enumerable over entities matching both T1 and T2.</summary>
    public RefEnumerable<T1, T2> IterateRef<T1, T2>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        if (s1 == null || s2 == null) return RefEnumerable<T1, T2>.Empty();
        return RefEnumerable<T1, T2>.From(s1, s2, markOnIterate: true);
    }

    /// <summary>Returns a span view of all components of type T.</summary>
    public ComponentSpan<T> GetSpan<T>()
    {
        var store = GetStore<T>(create: false);
        if (store == null || store.Count == 0) return default;
        return store.AsSpan();
    }

    /// <summary>Enumerates all (entity, component) pairs of type T.</summary>
    public IEnumerable<(int Entity, T Component)> Query<T>()
    {
        var store = GetStore<T>(create: false);
        if (store == null) yield break;
        foreach (var item in store.Enumerate())
            yield return item;
    }

    /// <summary>Enumerates entities that have both T1 and T2, iterating the smaller store first.</summary>
    public IEnumerable<(int Entity, T1 C1, T2 C2)> Query<T1, T2>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        if (s1 == null || s2 == null) yield break;
        if (s1.Count <= s2.Count)
        {
            foreach (var (e, c1) in s1.Enumerate())
                if (s2.TryGet(e, out var c2))
                    yield return (e, c1, c2);
        }
        else
        {
            foreach (var (e, c2) in s2.Enumerate())
                if (s1.TryGet(e, out var c1))
                    yield return (e, c1, c2);
        }
    }

    /// <summary>Enumerates entities that have T1, T2, and T3.</summary>
    public IEnumerable<(int Entity, T1 C1, T2 C2, T3 C3)> Query<T1, T2, T3>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        var s3 = GetStore<T3>(create: false);
        if (s1 == null || s2 == null || s3 == null) yield break;
        var c1 = s1.Count;
        var c2 = s2.Count;
        var c3 = s3.Count;
        int smallestCase = (c1 <= c2 && c1 <= c3) ? 1 : (c2 <= c1 && c2 <= c3) ? 2 : 3;
        if (smallestCase == 1)
        {
            foreach (var (e, comp1) in s1.Enumerate())
            {
                if (!s2.TryGet(e, out var comp2) || !s3.TryGet(e, out var comp3)) continue;
                yield return (e, comp1, comp2, comp3);
            }
        }
        else if (smallestCase == 2)
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

    /// <summary>Enumerates components of type T that match a predicate.</summary>
    public IEnumerable<(int Entity, T Component)> QueryWhere<T>(Func<T, bool> predicate)
    {
        foreach (var (entity, comp) in Query<T>())
            if (predicate(comp))
                yield return (entity, comp);
    }
}