using System.ComponentModel;
using System.Reflection;

using EnTTSharp.Entities;

namespace Engine.ECS;

public enum Tags
{
    Untagged,
    MainCamera,
    Respawn,
    Player,
    Finish,
    GameController,
}

public enum Layers
{
    Default,
    TransparentFX,
    IgnoreRayCast,
    Water,
    UI
}

public sealed partial class EntityData : EditorComponent
{
    private readonly EntityRegistry<EntityKey> _registry;

    public Guid ID = Guid.NewGuid();

    public EntityData Parent;

    public string Name;
    public bool IsActive = true;
    public bool IsStatic = false;
    [Hide] public bool IsHidden = false;
    public string Tag;
    public string Layer;

    public EntityManager EntityManager => _entityManager;
    private EntityManager _entityManager;

    public Transform Transform => _transform;
    private Transform _transform;

    public bool IsActiveInHierarchy => Parent is null ? IsActive : IsActive && (Parent.IsActiveInHierarchy && Parent.IsActive);

    public EntityData() { }

    public EntityData(EntityManager entityManager, EntityKey entityKey)
    {
        EntityData = this;
        EntityKey = entityKey;

        _entityManager = entityManager;
        _registry = _entityManager.Registry;

        EntityDataSystem.Register(this);
        
        _registry.AssignComponent(entityKey, this);

        _transform = AddComponent<Transform>();
    }

    public override void Dispose()
    {
        base.Dispose();

        IsActive = false;

        RemoveComponents<Component>();
    }
}

public sealed partial class EntityData : EditorComponent
{
    public T AddComponent<T>() where T : Component, new() =>
        (T)AddComponent(new T());

    public Component AddComponent(Type type) =>
        AddComponent((Component)Activator.CreateInstance(type));

    public Component AddComponent(Component component)
    {
        typeof(Component)
            .GetProperty("EntityData", BindingFlags.Instance | BindingFlags.Public)
            .SetValue(component, this);

        component.EntityKey = EntityKey;
        component.IsEnabled = true;

        component.OnRegister();

        _registry.AssignComponent(component.EntityKey, component);

        component.OnAwake();
        component.OnStart();

        return component;
    }

    public void RemoveComponent<T>() where T : Component
    {
        _registry.GetComponent(EntityKey, out T component);

        component.InvokeEventOnDestroy();

        _registry.RemoveComponent<T>(EntityKey);

        //_components.Remove(component);
    }

    public void RemoveComponents<T>() where T : Component
    {
        if (_registry.GetComponent(EntityKey, out T[] components))
            foreach (var component in components)
                component.OnDestroy();
    }

    public T GetComponent<T>() where T : Component
    {
        if (_registry.GetComponent(EntityKey, out T component))
            return component;
        else
            return null;
    }

    public T[] GetComponents<T>() where T : Component
    {
        if (_registry.GetComponent(EntityKey, out T[] components))
            return components;
        else
            return null;
    }

    public Component[] GetComponents() =>
        GetComponents<Component>();

    public bool CompareTag(params string[] tags)
    {
        // Check if the Entity has a specified tag.
        foreach (var tag in tags)
            // If the Entity has the tag, return true.
            if (Tag.Equals(tag))
                return true;

        // Return false if no matching tag is found.
        return false;
    }

    public string GetDebugInformation()
    {
        var info =
            $"""
            Name: {Name}
            ID: {ID}
            Scene: {EntityManager.Name}
            """;

        if (Parent is not null)
            info +=
                $"""

                Parent: {Parent.Name}
                Parent ID: {Parent.ID}
                """;

        return info;
    }
}

public sealed partial class EntityData : EditorComponent, ICloneable
{
    object ICloneable.Clone() =>
        Clone();

    public override EntityData Clone()
    {
        base.Clone();

        var newEntityKey = _registry.Create();

        // Create a new Entity object with a new ID and set its properties.
        var newEntityData = new EntityData(_entityManager, newEntityKey)
        {
            ID = Guid.NewGuid(), // Generate a new ID for the new Entity object.
            Name = Name,
            IsActive = IsActive,
            IsHidden = IsHidden,
            IsStatic = IsStatic,
            Layer = Layer,
            Tag = Tag
        };

        // Copy the original Entity object's Transform properties to the new Entity object.
        newEntityData.Transform.LocalPosition = Transform.LocalPosition;
        newEntityData.Transform.LocalRotation = Transform.LocalRotation;
        newEntityData.Transform.LocalScale = Transform.LocalScale;

        // Loop through the original Entity object's Components,
        // clone each one and register it to the new Entity object.
        foreach (var component in GetComponents())
        {
            if (component.GetType().Equals(typeof(EntityData))
             || component.GetType().Equals(typeof(Transform)))
                continue;

            // Clone the Component.
            var newComponent = component.Clone();
            // Call OnRegister method on the new Component.
            newComponent.OnRegister();
            // Add the new Component to the new Entity object.
            newEntityData.AddComponent(newComponent);
        }

        return newEntityData;
    }
}

/*
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
 */