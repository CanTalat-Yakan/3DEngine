namespace Engine;

public sealed partial class EcsWorld
{
    private readonly EntityPool _entities = new();
    private readonly Dictionary<Type, IComponentStore> _stores = new();
    
    private interface IComponentStore
    {
        int Count { get; }
        bool TryRemove(int entity, out IDisposable? disposable);
        void ClearChangedTicks();
    }
}