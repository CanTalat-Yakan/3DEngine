using System.Collections.Generic;

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

public sealed partial class Entity : IDisposable
{
    public Guid ID = Guid.NewGuid();

    public Entity Parent;

    public string Name;
    public bool IsEnabled = true;
    public bool IsStatic = false;
    [Hide] public bool IsHidden = false;
    public string Tag;
    public string Layer;

    public SceneManager SceneManager => Kernel.Instance.SceneManager;

    public Scene Scene { get => _scene ??= SceneManager.GetFromEntityID(ID); set => _scene = null; }
    private Scene _scene;

    public Transform Transform => _transform;
    private Transform _transform;

    public bool ActiveInHierarchy => Parent is null ? IsEnabled : IsEnabled && (Parent.ActiveInHierarchy && Parent.IsEnabled);

    public EventList<Component> Components => _components;
    private EventList<Component> _components = new();

    public Entity() =>
        // Add the Transform component to the Entity when initialized.
        AddComponent(_transform = new());

    public void Dispose()
    {
        // Disable the entity.
        IsEnabled = false;

        // Remove all components from the entity.
        RemoveComponents();
    }
}

public sealed partial class Entity
{
    public T AddComponent<T>() where T : Component, new() =>
        (T)AddComponent(new T());

    public Component AddComponent(Type type) =>
        AddComponent((Component)Activator.CreateInstance(type));

    public Component AddComponent(Component component)
    {
        // Add the component to the Entity's component list.
        _components.Add(component);

        // Assign this Entity to the component's Entity.
        component.Entity = this;
        // Enable the component by default.
        component.IsEnabled = true;
        
        // Call the Awake and Start method to initialize the component.
        component.OnAwake();
        component.OnStart();

        return component;
    }

    public void RemoveComponent(Component component)
    {
        // Invoke the component's OnDestroy event.
        component.InvokeEventOnDestroy();

        // Remove the component from the Entity's component list.
        _components.Remove(component);
    }

    public void RemoveComponents()
    {
        // Destroy all components associated with the Entity.
        foreach (var component in _components)
            component.Dispose();

        // Clear list of components associated with the Entity.
        _components.Clear();
    }

    public T GetComponent<T>() where T : Component
    {
        // Iterate through all components of the entity.
        foreach (var component in _components)
            // Check if the component type is equal to the type specified in the function.
            if (component.GetType().Equals(typeof(T)))
                // If a match is found, return the component as the specified type.
                return (T)component;

        // If no match is found, return null.
        return null;
    }

    public T[] GetComponents<T>() where T : Component
    {
        // Initialize an empty list of components.
        List<T> components = new();

        // Loop through all components of the Entity
        foreach (var component in _components)
            // If the component is of the specified type, add it to the list
            if (component.GetType().Equals(typeof(T)))
                components.Add((T)component);

        // Return the array of components of the specified type.
        return components.ToArray();
    }

    public Component[] GetComponents() =>
        _components.ToArray();

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
            Scene: {Scene.Name}
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

public sealed partial class Entity : ICloneable
{
    object ICloneable.Clone() =>
        Clone();

    public Entity Clone()
    {
        // Create a new Entity object with a new ID and set its properties.
        var newEntity = new Entity()
        {
            ID = Guid.NewGuid(), // Generate a new ID for the new Entity object.
            Name = Name,
            IsEnabled = IsEnabled,
            IsHidden = IsHidden,
            IsStatic = IsStatic,
            Layer = Layer,
            Tag = Tag
        };

        // Copy the original Entity object's Transform properties to the new Entity object.
        newEntity.Transform.LocalPosition = Transform.LocalPosition;
        newEntity.Transform.LocalRotation = Transform.LocalRotation;
        newEntity.Transform.LocalScale = Transform.LocalScale;

        // Loop through the original Entity object's Components,
        // clone each one and register it to the new Entity object.
        for (int i = 1; i < _components.Count; i++) // Skip transform.
        {
            // Clone the Component.
            var newComponent = _components[i].Clone();
            // Call OnRegister method on the new Component.
            newComponent.OnRegister();
            // Add the new Component to the new Entity object.
            newEntity.AddComponent(newComponent);
        }

        // Return the new Entity object.
        return newEntity;
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