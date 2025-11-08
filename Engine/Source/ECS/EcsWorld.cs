using System.Runtime.CompilerServices;

namespace Engine;

// ECS World implementation
public sealed class EcsWorld
{
    private int _nextEntity = 1;
    private readonly Dictionary<Type, IComponentStore> _stores = new();
    private int _currentTick; // default 0
    private readonly Stack<int> _free = new();
    private int[] _entityGenerations = Array.Empty<int>(); // generation per entity id (index == entity id)
    private const int FirstGeneration = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureGenerationCapacity(int id)
    {
        if (id < _entityGenerations.Length) return;
        int newSize = _entityGenerations.Length == 0 ? Math.Max(128, id + 1) : _entityGenerations.Length;
        while (id >= newSize) newSize *= 2;
        Array.Resize(ref _entityGenerations, newSize);
    }

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
 
    /// <summary>Spawns a new entity and returns its integer ID.</summary>
    public int Spawn()
    {
        int id = _free.Count > 0 ? _free.Pop() : _nextEntity++;
        EnsureGenerationCapacity(id);
        if (_entityGenerations[id] == 0) _entityGenerations[id] = FirstGeneration; // initialize first time
        return id;
    }

    /// <summary>Returns current generation for an entity id (0 if never used).</summary>
    public int GetGeneration(int entityId)
    {
        if ((uint)entityId >= (uint)_entityGenerations.Length) return 0;
        return _entityGenerations[entityId];
    }

    /// <summary>Removes an entity and all of its components, disposing IDisposable components.</summary>
    public void Despawn(int entity)
    {
        foreach (var store in _stores.Values)
            if (store.TryRemove(entity, out var disposable) && disposable is not null)
                try { disposable.Dispose(); } catch { }

        EnsureGenerationCapacity(entity);
        int g = _entityGenerations[entity];
        g = g == int.MaxValue ? FirstGeneration : g + 1;
        _entityGenerations[entity] = g;
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
        // Clear per-store changed flags each frame (bitset reset)
        foreach (var s in _stores.Values) s.ClearChangedTicks();
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

    // Internal typed store plumbing with per-world dictionary
    private ComponentStore<T> GetStore<T>(bool create = true)
    {
        if (_stores.TryGetValue(typeof(T), out var existing))
            return (ComponentStore<T>)existing;
        if (!create) return null!;
        var created = new ComponentStore<T>();
        _stores[typeof(T)] = created;
        return created;
    }


    private interface IComponentStore
    {
        int Count { get; }
        bool TryRemove(int entity, out IDisposable? disposable);
        void ClearChangedTicks();
    }

    internal sealed class ComponentStore<T> : IComponentStore
    {
        private int[] _denseEntities = Array.Empty<int>();
        private T[] _denseComponents = Array.Empty<T>();
        // Bitset of components changed this frame: one bit per dense index
        private long[] _changedBits = Array.Empty<long>();
        private int[] _sparse = Array.Empty<int>(); // maps entity -> dense index (or -1)
        private int _count;

        public int Count => _count;

        // Expose spans/arrays for advanced iteration (internal)
        internal ReadOnlySpan<int> EntitiesSpan() => _denseEntities.AsSpan(0, _count);
        internal int[] EntitiesArray => _denseEntities; // used in parallel ops
        internal T[] ComponentsArray => _denseComponents; // used in parallel ops
        // No direct array exposure for bits; use thread-safe setter

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnsureBitCapacity(int denseCapacity)
        {
            int words = (denseCapacity + 63) >> 6;
            if (_changedBits.Length < words)
                Array.Resize(ref _changedBits, words);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetBit(int index)
        {
            int word = index >> 6;
            int bit = index & 63;
            _changedBits[word] |= (1L << bit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearBit(int index)
        {
            int word = index >> 6;
            int bit = index & 63;
            _changedBits[word] &= ~(1L << bit);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetBit(int index)
        {
            int word = index >> 6;
            int bit = index & 63;
            return ((_changedBits[word] >> bit) & 1L) != 0;
        }

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
            EnsureBitCapacity(newCap);
        }

        public void Reserve(int componentCapacity, int maxEntityIdHint)
        {
            if (componentCapacity > _denseEntities.Length)
            {
                Array.Resize(ref _denseEntities, componentCapacity);
                Array.Resize(ref _denseComponents, componentCapacity);
                EnsureBitCapacity(componentCapacity);
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
            ClearBit(idx); // ensure not marked changed
            _sparse[entity] = idx;
        }

        public void Update(int entity, T component, int currentTick)
        {
            EnsureSparseCapacity(entity);
            int idx = _sparse[entity];
            if (idx >= 0)
            {
                _denseComponents[idx] = component!;
                SetBit(idx);
                return;
            }
            EnsureDenseCapacity();
            idx = _count++;
            _denseEntities[idx] = entity;
            _denseComponents[idx] = component!;
            SetBit(idx);
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
            entity < _sparse.Length && _sparse[entity] >= 0 && GetBit(_sparse[entity]);

        // Struct enumerable to avoid iterator allocations
        public ComponentEnumerable Enumerate() => new(this);

        public readonly struct ComponentEnumerable
        {
            private readonly ComponentStore<T> _store;

            public ComponentEnumerable(ComponentStore<T> store) => _store = store;

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
        public void MarkChangedByDenseIndex(int denseIndex, int tick) => SetBit(denseIndex);

        // Thread-safe bit set for parallel transforms
        public void MarkChangedByDenseIndexThreadSafe(int denseIndex)
        {
            int word = denseIndex >> 6;
            int bit = denseIndex & 63;
            Interlocked.Or(ref _changedBits[word], 1L << bit);
        }

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
                // move changed bit
                if (GetBit(lastIdx)) SetBit(idx); else ClearBit(idx);
                ClearBit(lastIdx);
                _sparse[_denseEntities[idx]] = idx;
            }
            _sparse[entity] = -1;
            _count--;
            return true;
        }

        // Non-disposing remove for explicit component removal
        public bool Remove(int entity)
        {
            if (entity >= _sparse.Length) return false;
            int idx = _sparse[entity];
            if (idx < 0) return false;
            int lastIdx = _count - 1;
            if (idx != lastIdx)
            {
                _denseComponents[idx] = _denseComponents[lastIdx];
                _denseEntities[idx] = _denseEntities[lastIdx];
                if (GetBit(lastIdx)) SetBit(idx); else ClearBit(idx);
                ClearBit(lastIdx);
                _sparse[_denseEntities[idx]] = idx;
            }
            _sparse[entity] = -1;
            _count--;
            return true;
        }

        // Reset only used portion
        public void ClearChangedTicks() => Array.Clear(_changedBits, 0, _changedBits.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int DenseIndexOf(int entity)
        {
            if ((uint)entity >= (uint)_sparse.Length) return -1;
            return _sparse[entity];
        }
    }

    // ==========================
    // Convenience APIs
    // ==========================

    public int Count<T>() => GetStore<T>(create: false)?.Count ?? 0;

    public bool Remove<T>(int entity)
    {
        var store = GetStore<T>(create: false);
        return store != null && store.Remove(entity);
    }

    public ReadOnlySpan<int> EntitiesWith<T>()
    {
        var store = GetStore<T>(create: false);
        return store == null ? ReadOnlySpan<int>.Empty : store.EntitiesSpan();
    }

    /// <summary>Parallel version of TransformEach; safe when transform is independent per element.</summary>
    public void ParallelTransformEach<T>(Func<int, T, T> transform)
    {
        var store = GetStore<T>(create: false);
        if (store == null || store.Count == 0) return;
        var count = store.Count;
        var ents = store.EntitiesArray;
        var comps = store.ComponentsArray;
        Parallel.For(0, count, i =>
        {
            var newVal = transform(ents[i], comps[i]);
            comps[i] = newVal;
            store.MarkChangedByDenseIndexThreadSafe(i);
        });
    }

    // ==========================
    // Zero-allocation ref iterators
    // ==========================

    public readonly ref struct RefComponent<T>
     {
         public readonly int Entity;
        private readonly Span<T> _components;
         private readonly int _index;
         public ref T Component => ref _components[_index];
         private RefComponent(int entity, Span<T> components, int index)
         { Entity = entity; _components = components; _index = index; }
         internal static RefComponent<T> Create(int entity, Span<T> comps, int index) => new(entity, comps, index);
     }
 
    public readonly ref struct RefEnumerable<T>
     {
         private readonly ReadOnlySpan<int> _entities;
         private readonly Span<T> _components;
         private readonly ComponentStore<T>? _store;
         private readonly bool _markOnIterate;
         private RefEnumerable(ReadOnlySpan<int> entities, Span<T> components, ComponentStore<T>? store, bool markOnIterate)
         { _entities = entities; _components = components; _store = store; _markOnIterate = markOnIterate; }
         public static RefEnumerable<T> From(ComponentSpan<T> span) => new(span.Entities, span.Components, null, false);
         internal static RefEnumerable<T> FromStore(ComponentStore<T> store, bool markOnIterate)
         {
             var span = store.AsSpan();
             return new RefEnumerable<T>(span.Entities, span.Components, store, markOnIterate);
         }
         public RefEnumerator GetEnumerator() => new(_entities, _components, _store, _markOnIterate);
         public ref struct RefEnumerator
         {
             private ReadOnlySpan<int> _entities;
             private Span<T> _components;
             private int _index;
            private readonly ComponentStore<T>? _store;
            private readonly bool _mark;
             internal RefEnumerator(ReadOnlySpan<int> entities, Span<T> components, ComponentStore<T>? store, bool mark)
             { _entities = entities; _components = components; _index = -1; _store = store; _mark = mark; }
             public RefComponent<T> Current
             {
                 get
                 {
                     if (_mark && _store != null)
                     {
                         // mark changed for current dense index before exposing ref
                         _store.MarkChangedByDenseIndex(_index, 0);
                     }
                     return RefComponent<T>.Create(_entities[_index], _components, _index);
                 }
             }
             public bool MoveNext()
             {
                 _index++;
                 return _index < _entities.Length;
             }
         }
     }
 
     public readonly ref struct RefComponents<T1, T2>
     {
         public readonly int Entity;
         private readonly ComponentStore<T1> _s1;
         private readonly ComponentStore<T2> _s2;
         private readonly int _e;
         private RefComponents(int entity, ComponentStore<T1> s1, ComponentStore<T2> s2)
         { Entity = entity; _s1 = s1; _s2 = s2; _e = entity; }
         internal static RefComponents<T1, T2> Create(int entity, ComponentStore<T1> s1, ComponentStore<T2> s2) => new(entity, s1, s2);
         public ref T1 C1 => ref _s1.GetRef(_e);
         public ref T2 C2 => ref _s2.GetRef(_e);
     }
 
     public readonly ref struct RefEnumerable<T1, T2>
     {
         private readonly ComponentStore<T1>? _a;
         private readonly ComponentStore<T2>? _b;
         private readonly int _which; // 0 = empty, 1 = iterate a, 2 = iterate b
        private readonly bool _markOnIterate;
         private RefEnumerable(ComponentStore<T1>? a, ComponentStore<T2>? b, int which, bool markOnIterate)
         { _a = a; _b = b; _which = which; _markOnIterate = markOnIterate; }
         internal static RefEnumerable<T1, T2> Empty() => new(null, null, 0, false);
         internal static RefEnumerable<T1, T2> From(ComponentStore<T1> a, ComponentStore<T2> b, bool markOnIterate)
             => new(a, b, a.Count <= b.Count ? 1 : 2, markOnIterate);
         public RefEnumerator GetEnumerator() => new(_a, _b, _which, _markOnIterate);
         public ref struct RefEnumerator
         {
             private readonly ComponentStore<T1>? _a;
             private readonly ComponentStore<T2>? _b;
             private readonly int _which;
            private readonly bool _mark;
             private int _i;
             internal RefEnumerator(ComponentStore<T1>? a, ComponentStore<T2>? b, int which, bool mark)
             { _a = a; _b = b; _which = which; _mark = mark; _i = -1; }
             public RefComponents<T1, T2> Current
             {
                 get
                 {
                     int e = _which == 1 ? _a!.EntityByDenseIndex(_i) : _b!.EntityByDenseIndex(_i);
                     if (_mark)
                     {
                         if (_which == 1)
                         {
                             _a.MarkChangedByDenseIndex(_i, 0);
                             int j = _b.DenseIndexOf(e);
                             if (j >= 0) _b.MarkChangedByDenseIndex(j, 0);
                         }
                         else
                         {
                             _b!.MarkChangedByDenseIndex(_i, 0);
                             int j = _a!.DenseIndexOf(e);
                             if (j >= 0) _a.MarkChangedByDenseIndex(j, 0);
                         }
                     }
                     return RefComponents<T1, T2>.Create(e, _a!, _b!);
                 }
             }
             public bool MoveNext()
             {
                 if (_which == 0 || _a == null || _b == null) return false;
                 do
                 {
                     _i++;
                     if (_which == 1)
                     {
                         if (_i >= _a.Count) return false;
                         int e = _a.EntityByDenseIndex(_i);
                         if (_b.Has(e)) return true;
                     }
                     else
                     {
                         if (_i >= _b.Count) return false;
                         int e = _b.EntityByDenseIndex(_i);
                         if (_a.Has(e)) return true;
                     }
                 } while (true);
             }
         }
     }
 
     public RefEnumerable<T> IterateRef<T>()
     {
         var store = GetStore<T>(create: false);
         if (store == null || store.Count == 0) return RefEnumerable<T>.From(default);
         return RefEnumerable<T>.FromStore(store, markOnIterate: true);
     }
 
     public RefEnumerable<T1, T2> IterateRef<T1, T2>()
     {
         var s1 = GetStore<T1>(create: false);
         var s2 = GetStore<T2>(create: false);
         if (s1 == null || s2 == null) return RefEnumerable<T1, T2>.Empty();
         return RefEnumerable<T1, T2>.From(s1, s2, markOnIterate: true);
     }
}
