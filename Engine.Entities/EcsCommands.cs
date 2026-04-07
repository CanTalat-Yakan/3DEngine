namespace Engine;

/// <summary>
/// Buffers mutating ECS operations (spawn/despawn/add/remove) to apply safely after system execution.
/// </summary>
/// <remarks>
/// Commands are deferred to prevent invalidating iterators or causing data races during system execution.
/// Typically flushed in <see cref="Stage.PostUpdate"/> by the <see cref="EcsPlugin"/>.
/// </remarks>
/// <example>
/// <code>
/// // Queue a new entity spawn with components
/// ctx.Cmd.Spawn((id, ecs) =>
/// {
///     ecs.Add(id, new Position { X = 0, Y = 0 });
///     ecs.Add(id, new Velocity { X = 1, Y = 0 });
/// });
///
/// // Queue despawn and component removal
/// ctx.Cmd.Despawn(entityId);
/// ctx.Cmd.Remove&lt;Health&gt;(entityId);
/// </code>
/// </example>
/// <seealso cref="EcsWorld"/>
/// <seealso cref="EcsPlugin"/>
/// <seealso cref="BehaviorContext"/>
public sealed class EcsCommands
{
    private readonly Queue<Action<EcsWorld>> _queue = new();

    /// <summary>Queues a spawn of a new entity built by the provided action.</summary>
    /// <param name="builder">
    /// A callback invoked with the new entity ID and the <see cref="EcsWorld"/>.
    /// Use it to add initial components to the spawned entity.
    /// </param>
    /// <returns>This <see cref="EcsCommands"/> instance for fluent chaining.</returns>
    public EcsCommands Spawn(Action<int, EcsWorld> builder)
    {
        _queue.Enqueue(world =>
        {
            var e = world.Spawn();
            builder(e, world);
        });
        return this;
    }

    /// <summary>Queues entity destruction by ID.</summary>
    /// <param name="entity">The entity ID to despawn.</param>
    /// <returns>This <see cref="EcsCommands"/> instance for fluent chaining.</returns>
    public EcsCommands Despawn(int entity)
    {
        _queue.Enqueue(world => world.Despawn(entity));
        return this;
    }

    /// <summary>Queues adding (or overwriting) a component on an entity.</summary>
    /// <typeparam name="T">The component type.</typeparam>
    /// <param name="entity">The entity ID to add the component to.</param>
    /// <param name="component">The component value to attach.</param>
    /// <returns>This <see cref="EcsCommands"/> instance for fluent chaining.</returns>
    public EcsCommands Add<T>(int entity, T component)
    {
        _queue.Enqueue(world => world.Add(entity, component));
        return this;
    }

    /// <summary>Queues removing a component from an entity.</summary>
    /// <typeparam name="T">The component type to remove.</typeparam>
    /// <param name="entity">The entity ID to remove the component from.</param>
    /// <returns>This <see cref="EcsCommands"/> instance for fluent chaining.</returns>
    public EcsCommands Remove<T>(int entity)
    {
        _queue.Enqueue(world => world.Remove<T>(entity));
        return this;
    }

    /// <summary>Applies all queued commands to the ECS world in FIFO order, emptying the buffer.</summary>
    /// <param name="world">The <see cref="EcsWorld"/> to apply commands against.</param>
    public void Apply(EcsWorld world)
    {
        while (_queue.Count > 0)
        {
            var cmd = _queue.Dequeue();
            cmd(world);
        }
    }
}