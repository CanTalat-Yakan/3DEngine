namespace Engine
{
    public sealed class BehaviorContext
    {
        public World World { get; }
        public EcsWorld Ecs { get; }
        public EcsCommands Cmd { get; }

        // Set by the behavior runner per invocation
        public int EntityId { get; internal set; }

        public BehaviorContext(World world)
        {
            World = world;
            Ecs = world.Resource<EcsWorld>();
            Cmd = world.Resource<EcsCommands>();
        }

        public T Res<T>() => World.Resource<T>();
    }
}
