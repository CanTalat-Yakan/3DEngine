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
    
    private interface IComponentStore
    {
        int Count { get; }
        bool TryRemove(int entity, out IDisposable? disposable);
        void ClearChangedTicks();
    }
}