namespace Engine;

/// <summary>
/// Per-invocation context providing convenient access to the <see cref="World"/>,
/// <see cref="EcsWorld"/>, <see cref="EcsCommands"/>, <see cref="Time"/>, and <see cref="Input"/>
/// resources, plus the current <see cref="EntityId"/> being processed.
/// </summary>
/// <remarks>
/// Created once per system invocation by the source-generated behavior runner.
/// Systems that iterate over entities set <see cref="EntityId"/> for each entity before
/// calling the behavior method.
/// </remarks>
/// <seealso cref="BehaviorAttribute"/>
/// <seealso cref="EcsWorld"/>
/// <seealso cref="EcsCommands"/>
public sealed class BehaviorContext
{
    /// <summary>Global state bag (resources and ECS world).</summary>
    public World World { get; }

    /// <summary>Direct access to the ECS world (components/entities).</summary>
    public EcsWorld Ecs { get; }

    /// <summary>Buffered ECS commands applied after systems run (spawn/despawn/add/remove).</summary>
    public EcsCommands Cmd { get; }
    
    /// <summary>Frame timing data (delta time, elapsed, FPS).</summary>
    public Time Time { get; }
    
    /// <summary>Current frame input state (keyboard, mouse, text).</summary>
    public Input Input { get; }

    /// <summary>Entity being processed for instance methods; <c>0</c> if not applicable.</summary>
    public int EntityId { get; set; }

    /// <summary>Creates a new <see cref="BehaviorContext"/> by resolving resources from the specified <paramref name="world"/>.</summary>
    /// <param name="world">The <see cref="World"/> from which to resolve ECS, commands, time, and input resources.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown if any required resource (<see cref="EcsWorld"/>, <see cref="EcsCommands"/>,
    /// <see cref="Time"/>, <see cref="Input"/>) is missing from the world.
    /// </exception>
    public BehaviorContext(World world)
    {
        World = world;
        Ecs = world.Resource<EcsWorld>();
        Cmd = world.Resource<EcsCommands>();
        Time = world.Resource<Time>();
        Input = world.Resource<Input>();
    }

    /// <summary>Gets a typed resource from the world.</summary>
    /// <typeparam name="T">The resource type to retrieve.</typeparam>
    /// <returns>The resource instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the resource is not found.</exception>
    public T Res<T>() where T : notnull => World.Resource<T>();
}