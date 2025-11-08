using System.Runtime.CompilerServices;

namespace Engine;

public sealed partial class EcsWorld
{
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

    public bool Has<T>(int entity)
    {
        var store = GetStore<T>(create: false);
        return store != null && store.Has(entity);
    }

    public bool Changed<T>(int entity)
    {
        var store = GetStore<T>(create: false);
        return store != null && store.ChangedThisFrame(entity, _currentTick);
    }

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

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ref T GetRef<T>(int entity)
    {
        var store = GetStore<T>(create: false) ??
                    throw new KeyNotFoundException($"Component {typeof(T).Name} store not found.");
        return ref store.GetRef(entity);
    }

    public IEnumerable<(int Entity, T Component)> Query<T>()
    {
        var store = GetStore<T>(create: false);
        if (store == null) yield break;
        foreach (var item in store.Enumerate())
            yield return item;
    }

    private ComponentStore<T> GetStore<T>(bool create = true)
    {
        if (_stores.TryGetValue(typeof(T), out var existing))
            return (ComponentStore<T>)existing;
        if (!create) return null!;
        var created = new ComponentStore<T>();
        _stores[typeof(T)] = created;
        return created;
    }

    internal sealed class ComponentStore<T> : IComponentStore
    {
        private int[] _denseEntities = Array.Empty<int>();
        private T[] _denseComponents = Array.Empty<T>();
        private long[] _changedBits = Array.Empty<long>();
        private int[] _sparse = Array.Empty<int>();
        private int _count;
        public int Count => _count;

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
            _changedBits[index >> 6] |= 1L << (index & 63);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ClearBit(int index)
        {
            _changedBits[index >> 6] &= ~(1L << (index & 63));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool GetBit(int index)
        {
            return ((_changedBits[index >> 6] >> (index & 63)) & 1L) != 0;
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
                _denseComponents[idx] = component!;
                return;
            }

            EnsureDenseCapacity();
            idx = _count++;
            _denseEntities[idx] = entity;
            _denseComponents[idx] = component!;
            ClearBit(idx);
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

        public void MarkChangedByDenseIndexThreadSafe(int denseIndex)
        {
            int word = denseIndex >> 6;
            int bit = denseIndex & 63;
            Interlocked.Or(ref _changedBits[word], 1L << bit);
        }

        public ComponentSpan<T> AsSpan() => new(_denseEntities.AsSpan(0, _count), _denseComponents.AsSpan(0, _count));

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
                _denseComponents[idx] = _denseComponents[lastIdx];
                _denseEntities[idx] = _denseEntities[lastIdx];
                if (GetBit(lastIdx)) SetBit(idx);
                else ClearBit(idx);
                ClearBit(lastIdx);
                _sparse[_denseEntities[idx]] = idx;
            }

            _sparse[entity] = -1;
            _count--;
            return true;
        }

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
                if (GetBit(lastIdx)) SetBit(idx);
                else ClearBit(idx);
                ClearBit(lastIdx);
                _sparse[_denseEntities[idx]] = idx;
            }

            _sparse[entity] = -1;
            _count--;
            return true;
        }

        public void ClearChangedTicks() => Array.Clear(_changedBits, 0, _changedBits.Length);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int DenseIndexOf(int entity)
        {
            if ((uint)entity >= (uint)_sparse.Length) return -1;
            return _sparse[entity];
        }

        internal ReadOnlySpan<int> EntitiesSpan() => _denseEntities.AsSpan(0, _count);
        internal int[] EntitiesArray => _denseEntities;
        internal T[] ComponentsArray => _denseComponents;
    }

    // Span struct used externally for high-performance iteration
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
}