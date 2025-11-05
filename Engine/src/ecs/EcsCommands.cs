namespace Engine;

public sealed class EcsCommands
{
    private readonly Queue<Action<EcsWorld>> _queue = new();
    public void Spawn(Action<int, EcsWorld> builder)
        => _queue.Enqueue(world => { var e = world.Spawn(); builder(e, world); });

    public void Despawn(int entity)
        => _queue.Enqueue(world => world.Despawn(entity));

    public void Add<T>(int entity, T component)
        => _queue.Enqueue(world => world.Add(entity, component));

    public void Apply(EcsWorld world)
    {
        while (_queue.Count > 0)
        {
            var cmd = _queue.Dequeue();
            cmd(world);
        }
    }
}

