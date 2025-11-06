namespace Engine.Behaviour
{
    public sealed class BehaviourContext
    {
        public World World { get; }
        public ECSWorld Ecs { get; }
        public ECSCommands Cmd { get; }

        // Set by the behaviour runner per invocation
        public int EntityId { get; internal set; }

        public BehaviourContext(World world)
        {
            World = world;
            Ecs = world.Resource<ECSWorld>();
            Cmd = world.Resource<ECSCommands>();
        }

        public T Res<T>() => World.Resource<T>();
    }
}
