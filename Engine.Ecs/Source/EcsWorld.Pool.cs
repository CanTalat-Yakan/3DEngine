namespace Engine;

/// <summary>Generational entity ID allocator with free-list reuse.</summary>
internal sealed class EntityPool
{
    private const int FirstGeneration = 1;
    private int _nextEntity = 1;
    private readonly Stack<int> _free = new();
    private int[] _generations = System.Array.Empty<int>();

    /// <summary>Allocates (or reuses) an entity id and returns it. Initializes generation if first time.</summary>
    public int Spawn()
    {
        int id = _free.Count > 0 ? _free.Pop() : _nextEntity++;
        EnsureCapacity(id);
        if (_generations[id] == 0) _generations[id] = FirstGeneration;
        return id;
    }

    /// <summary>Gets current generation for id, or 0 if never allocated.</summary>
    public int GetGeneration(int id)
    {
        if ((uint)id >= (uint)_generations.Length) return 0;
        return _generations[id];
    }

    /// <summary>Despawn id (caller must have removed components). Increments generation and recycles id.</summary>
    public void Despawn(int id)
    {
        EnsureCapacity(id);
        int g = _generations[id];
        if (g == 0) g = FirstGeneration;
        g = g == int.MaxValue ? FirstGeneration : g + 1;
        _generations[id] = g;
        _free.Push(id);
    }

    private void EnsureCapacity(int id)
    {
        if (id < _generations.Length) return;
        int newSize = _generations.Length == 0 ? System.Math.Max(128, id + 1) : _generations.Length;
        while (id >= newSize) newSize *= 2;
        System.Array.Resize(ref _generations, newSize);
    }
}
