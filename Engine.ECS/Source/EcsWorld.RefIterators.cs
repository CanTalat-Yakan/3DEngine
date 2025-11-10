namespace Engine;

public sealed partial class EcsWorld
{
    // Zero-allocation ref iterator (single component)
    public readonly ref struct RefComponent<T>
    {
        public readonly int Entity;
        private readonly Span<T> _components;
        private readonly int _index;
        public ref T Component => ref _components[_index];

        private RefComponent(int entity, Span<T> components, int index)
        {
            Entity = entity;
            _components = components;
            _index = index;
        }

        internal static RefComponent<T> Create(int entity, Span<T> comps, int index) => new(entity, comps, index);
    }

    public readonly ref struct RefEnumerable<T>
    {
        private readonly ReadOnlySpan<int> _entities;
        private readonly Span<T> _components;
        private readonly ComponentStore<T>? _store;
        private readonly bool _markOnIterate;

        private RefEnumerable(ReadOnlySpan<int> entities, Span<T> components, ComponentStore<T>? store, bool markOnIterate)
        {
            _entities = entities;
            _components = components;
            _store = store;
            _markOnIterate = markOnIterate;
        }

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
            {
                _entities = entities;
                _components = components;
                _index = -1;
                _store = store;
                _mark = mark;
            }

            public RefComponent<T> Current
            {
                get
                {
                    if (_mark && _store != null) _store.MarkChangedByDenseIndex(_index, 0);
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

    // Zero-allocation ref iterator (two components)
    public readonly ref struct RefComponents<T1, T2>
    {
        public readonly int Entity;
        private readonly ComponentStore<T1> _s1;
        private readonly ComponentStore<T2> _s2;
        private readonly int _e;

        private RefComponents(int entity, ComponentStore<T1> s1, ComponentStore<T2> s2)
        {
            Entity = entity;
            _s1 = s1;
            _s2 = s2;
            _e = entity;
        }

        internal static RefComponents<T1, T2> Create(int entity, ComponentStore<T1> s1, ComponentStore<T2> s2) =>
            new(entity, s1, s2);

        public ref T1 C1 => ref _s1.GetRef(_e);
        public ref T2 C2 => ref _s2.GetRef(_e);
    }

    public readonly ref struct RefEnumerable<T1, T2>
    {
        private readonly ComponentStore<T1>? _a;
        private readonly ComponentStore<T2>? _b;
        private readonly int _which;
        private readonly bool _markOnIterate;

        private RefEnumerable(ComponentStore<T1>? a, ComponentStore<T2>? b, int which, bool markOnIterate)
        {
            _a = a;
            _b = b;
            _which = which;
            _markOnIterate = markOnIterate;
        }

        internal static RefEnumerable<T1, T2> Empty() => new(null, null, 0, false);

        internal static RefEnumerable<T1, T2> From(ComponentStore<T1> a, ComponentStore<T2> b, bool markOnIterate) =>
            new(a, b, a.Count <= b.Count ? 1 : 2, markOnIterate);

        public RefEnumerator GetEnumerator() => new(_a, _b, _which, _markOnIterate);

        public ref struct RefEnumerator
        {
            private readonly ComponentStore<T1>? _a;
            private readonly ComponentStore<T2>? _b;
            private readonly int _which;
            private readonly bool _mark;
            private int _i;

            internal RefEnumerator(ComponentStore<T1>? a, ComponentStore<T2>? b, int which, bool mark)
            {
                _a = a;
                _b = b;
                _which = which;
                _mark = mark;
                _i = -1;
            }

            public RefComponents<T1, T2> Current
            {
                get
                {
                    int e = _which == 1 ? _a!.EntityByDenseIndex(_i) : _b!.EntityByDenseIndex(_i);
                    if (_mark)
                    {
                        if (_which == 1)
                        {
                            _a!.MarkChangedByDenseIndex(_i, 0);
                            int j = _b!.DenseIndexOf(e);
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
}