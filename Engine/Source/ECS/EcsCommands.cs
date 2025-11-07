namespace Engine;

public sealed class EcsCommands
{
    private readonly Queue<Action<EcsWorld>> _queue = new();

    public EcsCommands Spawn(Action<int, EcsWorld> builder)
    {
        _queue.Enqueue(world =>
        {
            var e = world.Spawn();
            builder(e, world);
        });
        return this;
    }

    public EcsCommands Despawn(int entity)
    {
        _queue.Enqueue(world => world.Despawn(entity));
        return this;
    }

    public EcsCommands Add<T>(int entity, T component)
    {
        _queue.Enqueue(world => world.Add(entity, component));
        return this;
    }

    public void Apply(EcsWorld world)
    {
        while (_queue.Count > 0)
        {
            var cmd = _queue.Dequeue();
            cmd(world);
        }
    }
}