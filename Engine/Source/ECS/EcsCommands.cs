namespace Engine;

/// <summary>Buffers mutating ECS operations (spawn/despawn/add) to apply safely after system execution.</summary>
public sealed class EcsCommands
{
    private readonly Queue<Action<EcsWorld>> _queue = new();

    /// <summary>Queues a spawn of a new entity built by provided action (id-only).</summary>
    public EcsCommands Spawn(Action<int, EcsWorld> builder)
    {
        _queue.Enqueue(world =>
        {
            var e = world.Spawn();
            builder(e, world);
        });
        return this;
    }

    /// <summary>Queues entity despawn by id.</summary>
    public EcsCommands Despawn(int entity)
    {
        _queue.Enqueue(world => world.Despawn(entity));
        return this;
    }

    /// <summary>Queues adding a component to an entity by id.</summary>
    public EcsCommands Add<T>(int entity, T component)
    {
        _queue.Enqueue(world => world.Add(entity, component));
        return this;
    }

    /// <summary>Applies all queued commands to the ECS world, emptying the buffer.</summary>
    public void Apply(EcsWorld world)
    {
        while (_queue.Count > 0)
        {
            var cmd = _queue.Dequeue();
            cmd(world);
        }
    }
}