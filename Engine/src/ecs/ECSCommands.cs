namespace Engine;

public sealed class ECSCommands
{
    private readonly Queue<Action<ECSWorld>> _queue = new();
    public ECSCommands Spawn(Action<int, ECSWorld> builder)
    { _queue.Enqueue(world => { var e = world.Spawn(); builder(e, world); }); return this; }

    public ECSCommands Despawn(int entity)
    { _queue.Enqueue(world => world.Despawn(entity)); return this; }

    public ECSCommands Add<T>(int entity, T component)
    { _queue.Enqueue(world => world.Add(entity, component)); return this; }

    public void Apply(ECSWorld world)
    {
        while (_queue.Count > 0)
        {
            var cmd = _queue.Dequeue();
            cmd(world);
        }
    }
}
