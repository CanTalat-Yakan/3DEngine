using System.Runtime.CompilerServices;

namespace Engine;

public sealed partial class EcsWorld
{
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
        private readonly SparseSet<T> _set = new();
        public int Count => _set.Count;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Reserve(int componentCapacity, int maxEntityIdHint)
            => _set.Reserve(componentCapacity, maxEntityIdHint);

        public void Add(int entity, T component) => _set.Add(entity, component);

        public void Update(int entity, T component, int currentTick) => _set.Update(entity, component);

        public bool Has(int entity) => _set.Has(entity);

        public bool TryGet(int entity, out T value) => _set.TryGet(entity, out value!);

        public bool ChangedThisFrame(int entity, int currentTick) => _set.ChangedThisFrame(entity);

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
                    (_store._set.EntityByDenseIndex(_index), _store._set.ComponentRefByDenseIndex(_index));

                public bool MoveNext()
                {
                    _index++;
                    return _index < _store.Count;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetRef(int entity) => ref _set.GetRef(entity);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int EntityByDenseIndex(int denseIndex) => _set.EntityByDenseIndex(denseIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T ComponentRefByDenseIndex(int denseIndex) => ref _set.ComponentRefByDenseIndex(denseIndex);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void MarkChangedByDenseIndex(int denseIndex, int tick) => _set.MarkChangedByDenseIndex(denseIndex);

        public void MarkChangedByDenseIndexThreadSafe(int denseIndex) => _set.MarkChangedByDenseIndexThreadSafe(denseIndex);

        public ComponentSpan<T> AsSpan()
        {
            _set.GetSpan(out var e, out var c);
            return new ComponentSpan<T>(e, c);
        }

        public bool TryRemove(int entity, out IDisposable? disposable) => _set.TryRemove(entity, out disposable);

        public bool Remove(int entity) => _set.Remove(entity);

        public void ClearChangedTicks() => _set.ClearChangedTicks();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal int DenseIndexOf(int entity) => _set.DenseIndexOf(entity);

        internal ReadOnlySpan<int> EntitiesSpan() => _set.EntitiesSpan();
        internal int[] EntitiesArray => _set.EntitiesArray;
        internal T[] ComponentsArray => _set.ComponentsArray;
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