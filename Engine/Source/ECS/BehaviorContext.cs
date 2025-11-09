namespace Engine;

/// <summary>Per-invocation context providing World, ECS, Commands, and current EntityId.</summary>
public sealed class BehaviorContext
{
    /// <summary>Global state bag (resources and ECS world).</summary>
    public World World { get; }

    /// <summary>Direct access to the ECS world (components/entities).</summary>
    public EcsWorld Ecs { get; }

    /// <summary>Buffered ECS commands applied after systems run.</summary>
    public EcsCommands Cmd { get; }
    
    /// <summary>Frame timing data.</summary>
    public Time Time { get; }
    
    /// <summary>Input state.</summary>
    public Input Input { get; }

    /// <summary>Entity being processed for instance methods; 0 if not applicable.</summary>
    public int EntityId { get; internal set; }

    public BehaviorContext(World world)
    {
        World = world;
        Ecs = world.Resource<EcsWorld>();
        Cmd = world.Resource<EcsCommands>();
        Time = world.Resource<Time>();
        Input = world.Resource<Input>();
    }

    /// <summary>Gets a typed resource from the world.</summary>
    public T Res<T>() => World.Resource<T>();
}