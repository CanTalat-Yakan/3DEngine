// ReSharper disable once CheckNamespace

namespace Engine;

using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

/// <summary>Lightweight ECS storage managing entities, components, and per-frame change tracking.
/// Optimized with sparse-set component stores for cache-friendly iteration and O(1) lookups.</summary>
public sealed class EcsWorld
{
    private int _nextEntity = 1;
    private readonly Dictionary<Type, IComponentStore> _stores = new();
    private int _currentTick; // default 0
    private readonly Stack<int> _free = new();

    /// <summary>Provides direct span access to entities and components of a given type.</summary>
    public readonly ref struct ComponentSpan<T>
    {
        public readonly ReadOnlySpan<int> Entities;
        public readonly Span<T> Components;
        public bool IsValid => !Entities.IsEmpty;

        public ComponentSpan(ReadOnlySpan<int> entities, Span<T> components)
        {
            Entities = entities;
            Components = components;
        }
    }

    /// <summary>Spawns a new entity id (reuses from free list if available).</summary>
    public int Spawn() => _free.Count > 0 ? _free.Pop() : _nextEntity++;

    /// <summary>Removes an entity and all of its components, disposing IDisposable components.</summary>
    public void Despawn(int entity)
    {
        foreach (var store in _stores.Values)
            if (store.TryRemove(entity, out var disposable) && disposable is not null)
                try
                {
                    disposable.Dispose();
                }
                catch
                {
                    /* swallow disposal exceptions */
                }

        _free.Push(entity);
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

    /// <summary>Gets a ref to component T for entity; throws if missing.</summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef<T>(int entity)
    {
        var store = GetStore<T>(create: false) ??
                    throw new KeyNotFoundException($"Component {typeof(T).Name} store not found.");
        return ref store.GetRef(entity);
    }

    /// <summary>Enumerates all entities with component T (compatibility wrapper; for performance prefer GetSpan or foreach over GetStore().Enumerate()).</summary>
    public IEnumerable<(int Entity, T Component)> Query<T>()
    {
        var store = GetStore<T>(create: false);
        if (store == null) yield break;
        foreach (var item in store.Enumerate())
            yield return item;
    }

    /// <summary>Enumerates entities having both components T1 and T2 using sparse-set lookup.</summary>
    public IEnumerable<(int Entity, T1 C1, T2 C2)> Query<T1, T2>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        if (s1 == null || s2 == null) yield break;
        // iterate smaller dense set
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

    /// <summary>Enumerates entities having components T1, T2 and T3 via smallest dense set.</summary>
    public IEnumerable<(int Entity, T1 C1, T2 C2, T3 C3)> Query<T1, T2, T3>()
    {
        var s1 = GetStore<T1>(create: false);
        var s2 = GetStore<T2>(create: false);
        var s3 = GetStore<T3>(create: false);
        if (s1 == null || s2 == null || s3 == null) yield break;

        // pick the smallest count
        var c1 = s1.Count;
        var c2 = s2.Count;
        var c3 = s3.Count;
        int smallestCase;
        if (c1 <= c2 && c1 <= c3) smallestCase = 1;
        else if (c2 <= c1 && c2 <= c3) smallestCase = 2;
        else smallestCase = 3;

        if (smallestCase == 1)
            foreach (var (e, comp1) in s1.Enumerate())
            {
                if (!s2.TryGet(e, out var comp2) || !s3.TryGet(e, out var comp3)) continue;
                yield return (e, comp1, comp2, comp3);
            }
        else if (smallestCase == 2)
            foreach (var (e, comp2) in s2.Enumerate())
            {
                if (!s1.TryGet(e, out var comp1) || !s3.TryGet(e, out var comp3)) continue;
                yield return (e, comp1, comp2, comp3);
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
        for (int i = 0; i < store.Count; i++)
        {
            ref var comp = ref store.ComponentRefByDenseIndex(i);
            var newVal = transform(store.EntityByDenseIndex(i), comp);
            comp = newVal;
            store.MarkChangedByDenseIndex(i, _currentTick);
        }
    }

    /// <summary>Provides a span-based view over components of type T for zero-allocation iteration/mutation.</summary>
    public ComponentSpan<T> GetSpan<T>()
    {
        var store = GetStore<T>(create: false);
        if (store == null || store.Count == 0) return default;
        return store.AsSpan();
    }

    /// <summary>Pre-reserves capacity for a component type to reduce resizing.</summary>
    public void Reserve<T>(int componentCapacity, int maxEntityIdHint = 0)
    {
        var store = GetStore<T>();
        store.Reserve(componentCapacity, maxEntityIdHint);
    }

    // Internal typed store plumbing with static generic cache for fast lookup
    private ComponentStore<T> GetStore<T>(bool create = true)
    {
        var cached = StoreCache<T>.Store;
        if (cached != null || !create) return cached!;
        var created = new ComponentStore<T>();
        StoreCache<T>.Store = created;
        _stores[typeof(T)] = created;
        return created;
    }

    private static class StoreCache<T>
    {
        public static ComponentStore<T>? Store;
    }

    private interface IComponentStore
    {
        int Count { get; }
        bool TryRemove(int entity, out IDisposable? disposable);
        void ClearChangedTicks();
    }

    private sealed class ComponentStore<T> : IComponentStore
    {
        private int[] _denseEntities = Array.Empty<int>();
        private T[] _denseComponents = Array.Empty<T>();
        private int[] _changedTicks = Array.Empty<int>();
        private int[] _sparse = Array.Empty<int>(); // maps entity -> dense index (or -1)
        private int _count;

        public int Count => _count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureSparseCapacity(int entity)
        {
            if (entity < _sparse.Length) return;
            int newSize = _sparse.Length == 0 ? Math.Max(entity + 1, 128) : _sparse.Length;
            while (entity >= newSize) newSize *= 2;
            int oldLen = _sparse.Length;
            Array.Resize(ref _sparse, newSize);
            for (int i = oldLen; i < newSize; i++) _sparse[i] = -1;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureDenseCapacity()
        {
            if (_count < _denseEntities.Length) return;
            int newCap = _count == 0 ? 128 : _count * 2;
            Array.Resize(ref _denseEntities, newCap);
            Array.Resize(ref _denseComponents, newCap);
            Array.Resize(ref _changedTicks, newCap);
        }

        public void Reserve(int componentCapacity, int maxEntityIdHint)
        {
            if (componentCapacity > _denseEntities.Length)
            {
                Array.Resize(ref _denseEntities, componentCapacity);
                Array.Resize(ref _denseComponents, componentCapacity);
                Array.Resize(ref _changedTicks, componentCapacity);
            }

            if (maxEntityIdHint > _sparse.Length)
            {
                int oldLen = _sparse.Length;
                Array.Resize(ref _sparse, maxEntityIdHint + 1);
                for (int i = oldLen; i < _sparse.Length; i++) _sparse[i] = -1;
            }
        }

        public void Add(int entity, T component)
        {
            EnsureSparseCapacity(entity);
            int idx = _sparse[entity];
            if (idx >= 0)
            {
                _denseComponents[idx] = component!; // overwrite without marking changed
                return;
            }

            EnsureDenseCapacity();
            idx = _count++;
            _denseEntities[idx] = entity;
            _denseComponents[idx] = component!;
            _changedTicks[idx] = 0; // not changed this frame yet
            _sparse[entity] = idx;
        }

        public void Update(int entity, T component, int currentTick)
        {
            EnsureSparseCapacity(entity);
            int idx = _sparse[entity];
            if (idx >= 0)
            {
                _denseComponents[idx] = component!;
                _changedTicks[idx] = currentTick;
                return;
            }

            EnsureDenseCapacity();
            idx = _count++;
            _denseEntities[idx] = entity;
            _denseComponents[idx] = component!;
            _changedTicks[idx] = currentTick;
            _sparse[entity] = idx;
        }

        public bool Has(int entity) => entity < _sparse.Length && _sparse[entity] >= 0;

        public bool TryGet(int entity, out T value)
        {
            if (entity < _sparse.Length)
            {
                int idx = _sparse[entity];
                if (idx >= 0)
                {
                    value = _denseComponents[idx];
                    return true;
                }
            }

            value = default!;
            return false;
        }

        public bool ChangedThisFrame(int entity, int currentTick) => 
            entity < _sparse.Length && _sparse[entity] >= 0 && _changedTicks[_sparse[entity]] == currentTick;

        // Struct enumerable to avoid iterator allocations
        public ComponentEnumerable Enumerate() => new(this);

        public readonly struct ComponentEnumerable
        {
            private readonly ComponentStore<T> _store;

            public ComponentEnumerable(ComponentStore<T> store) =>
                _store = store;

            public Enumerator GetEnumerator() => new(_store);

            public struct Enumerator
            {
                private readonly ComponentStore<T> _store;
                private int _index;

                internal Enumerator(ComponentStore<T> store)
                {
                    _store = store;
                    _index = -1;
                }

                public (int Entity, T Component) Current =>
                    (_store._denseEntities[_index], _store._denseComponents[_index]);

                public bool MoveNext()
                {
                    _index++;
                    return _index < _store._count;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRef(int entity)
        {
            if (entity >= _sparse.Length)
                throw new KeyNotFoundException($"Entity {entity} does not have component {typeof(T).Name}.");
            int idx = _sparse[entity];
            if (idx < 0) throw new KeyNotFoundException($"Entity {entity} does not have component {typeof(T).Name}.");
            return ref _denseComponents[idx];
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EntityByDenseIndex(int denseIndex) => _denseEntities[denseIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ComponentRefByDenseIndex(int denseIndex) => ref _denseComponents[denseIndex];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkChangedByDenseIndex(int denseIndex, int tick) => _changedTicks[denseIndex] = tick;

        public ComponentSpan<T> AsSpan() => 
            new(_denseEntities.AsSpan(0, _count), _denseComponents.AsSpan(0, _count));

        public bool TryRemove(int entity, out IDisposable? disposable)
        {
            disposable = null;
            if (entity >= _sparse.Length) return false;
            int idx = _sparse[entity];
            if (idx < 0) return false;

            var comp = _denseComponents[idx];
            if (comp is IDisposable d) disposable = d;

            int lastIdx = _count - 1;
            if (idx != lastIdx)
            {
                // swap-back to keep dense packed
                _denseComponents[idx] = _denseComponents[lastIdx];
                _denseEntities[idx] = _denseEntities[lastIdx];
                _changedTicks[idx] = _changedTicks[lastIdx];
                _sparse[_denseEntities[idx]] = idx;
            }

            _sparse[entity] = -1;
            _count--;
            return true;
        }

        public void ClearChangedTicks()
        {
            // Reset only used portion
            Array.Clear(_changedTicks, 0, _count);
        }
    }
}