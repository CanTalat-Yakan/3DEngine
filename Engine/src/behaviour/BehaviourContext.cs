using Engine;

namespace Engine.Behaviour
{
    public sealed class BehaviourContext
    {
        public World World { get; }
        public EcsWorld Ecs { get; }
        public EcsCommands Cmd { get; }

        // Set by the behaviour runner per invocation
        public int EntityId { get; internal set; }

        public BehaviourContext(World world)
        {
            World = world;
            Ecs = world.Resource<EcsWorld>();
            Cmd = world.Resource<EcsCommands>();
        }

        public T Res<T>() => World.Resource<T>();
    }
}
