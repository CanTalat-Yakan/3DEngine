namespace Engine
{
    public sealed class BehaviorContext
    {
        public World World { get; }
        public ECSWorld Ecs { get; }
        public ECSCommands Cmd { get; }

        // Set by the behavior runner per invocation
        public int EntityId { get; internal set; }

        public BehaviorContext(World world)
        {
            World = world;
            Ecs = world.Resource<ECSWorld>();
            Cmd = world.Resource<ECSCommands>();
        }

        public T Res<T>() => World.Resource<T>();
    }
}
