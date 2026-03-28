using System.Runtime.CompilerServices;

namespace Engine;

/// <summary>
/// A packed sparse set for mapping entity ids to dense component arrays with O(1) add/remove/lookup
/// and a per-dense-index change bitset for cheap frame-change tracking.
/// Intended for ECS component storage.
/// </summary>
internal sealed class SparseSet<T>
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

    // Add without marking changed (plain set)
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

    // Update (or add) and mark changed
    public void Update(int entity, T component)
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

    public bool ChangedThisFrame(int entity) => entity < _sparse.Length && _sparse[entity] >= 0 && GetBit(_sparse[entity]);

    public readonly struct ComponentEnumerable
    {
        private readonly SparseSet<T> _set;
        public ComponentEnumerable(SparseSet<T> set) => _set = set;
        public Enumerator GetEnumerator() => new(_set);

        public struct Enumerator
        {
            private readonly SparseSet<T> _set;
            private int _index;
            internal Enumerator(SparseSet<T> set) { _set = set; _index = -1; }
            public (int Entity, T Component) Current => (_set._denseEntities[_index], _set._denseComponents[_index]);
            public bool MoveNext() { _index++; return _index < _set._count; }
        }
    }

    public ComponentEnumerable Enumerate() => new(this);

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
    public void MarkChangedByDenseIndex(int denseIndex) => SetBit(denseIndex);

    public void MarkChangedByDenseIndexThreadSafe(int denseIndex)
    {
        int word = denseIndex >> 6;
        int bit = denseIndex & 63;
        Interlocked.Or(ref _changedBits[word], 1L << bit);
    }

    public void GetSpan(out ReadOnlySpan<int> entities, out Span<T> components)
    {
        entities = _denseEntities.AsSpan(0, _count);
        components = _denseComponents.AsSpan(0, _count);
    }

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
            if (GetBit(lastIdx)) SetBit(idx); else ClearBit(idx);
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
            if (GetBit(lastIdx)) SetBit(idx); else ClearBit(idx);
            ClearBit(lastIdx);
            _sparse[_denseEntities[idx]] = idx;
        }

        _sparse[entity] = -1;
        _count--;
        return true;
    }

    public void ClearChangedTicks() => Array.Clear(_changedBits, 0, _changedBits.Length);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int DenseIndexOf(int entity)
    {
        if ((uint)entity >= (uint)_sparse.Length) return -1;
        return _sparse[entity];
    }

    public int[] EntitiesArray => _denseEntities;
    public T[] ComponentsArray => _denseComponents;
    public ReadOnlySpan<int> EntitiesSpan() => _denseEntities.AsSpan(0, _count);
}
