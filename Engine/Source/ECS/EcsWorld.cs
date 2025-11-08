using System.Runtime.CompilerServices;

namespace Engine;

public sealed partial class EcsWorld
{
    // Core fields (entity lifecycle + global state)
    private int _nextEntity = 1;
    private readonly Dictionary<Type, IComponentStore> _stores = new();
    private int _currentTick; // frame counter for change tracking
    private readonly Stack<int> _free = new();
    private int[] _entityGenerations = Array.Empty<int>(); // generation per entity id
    private const int FirstGeneration = 1;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void EnsureGenerationCapacity(int id)
    {
        if (id < _entityGenerations.Length) return;
        int newSize = _entityGenerations.Length == 0 ? Math.Max(128, id + 1) : _entityGenerations.Length;
        while (id >= newSize) newSize *= 2;
        Array.Resize(ref _entityGenerations, newSize);
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

    // --- Forward declarations for partials ---
    private interface IComponentStore
    {
        int Count { get; }
        bool TryRemove(int entity, out IDisposable? disposable);
        void ClearChangedTicks();
    }
}