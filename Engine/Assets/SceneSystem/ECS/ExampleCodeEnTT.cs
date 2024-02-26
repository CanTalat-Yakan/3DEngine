using EnTTSharp.Entities;

namespace Engine.ECS;

internal class ExampleCodeEnTT
{
    public readonly struct Position
    {
        public readonly double X;
        public readonly double Y;

        public Position(double x, double y)
        {
            X = x;
            Y = y;
        }
    }

    public readonly struct Velocity
    {
        public readonly double DeltaX;
        public readonly double DeltaY;

        public Velocity(double deltaX, double deltaY)
        {
            DeltaX = deltaX;
            DeltaY = deltaY;
        }
    }

    public void UpdatePosition(EntityRegistry<EntityKey> registry, TimeSpan deltaTime)
    {
        // view contains all the entities that have both a position and a velocity component ...
        var view = registry.View<Velocity, Position>();

        foreach (var entity in view)
            if (view.GetComponent(entity, out Position pos) &&
                view.GetComponent(entity, out Velocity velocity))
            {
                Position posChanged = new(pos.X + velocity.DeltaX * deltaTime.TotalSeconds,
                                          pos.Y + velocity.DeltaY * deltaTime.TotalSeconds);
                registry.AssignComponent(entity, in posChanged);
            }
    }

    public void ClearVelocity(EntityRegistry<EntityKey> registry)
    {
        var view = registry.View<Velocity>();

        foreach (var entity in view)
            registry.AssignComponent(entity, new Velocity(0, 0));
    }

    public void Start()
    {
        Random rnd = new();

        // Define the entity key factory function
        Func<byte, int, EntityKey> entityKeyFactory = (generation, index) => new EntityKey(generation, index);
        // Instantiate the registry with the desired maxAge and the entityKeyFactory function
        EntityRegistry<EntityKey> registry = new(10, entityKeyFactory);

        registry.Register<Velocity>();
        registry.Register<Position>();

        for (int x = 0; x < 10; x += 1)
        {
            var entity = registry.Create();
            registry.AssignComponent<Position>(entity);
            if ((x % 2) == 0)
                registry.AssignComponent(entity, new Velocity(rnd.NextDouble(), rnd.NextDouble()));
        }

        UpdatePosition(registry, TimeSpan.FromSeconds(0.24));
        ClearVelocity(registry);
    }
}