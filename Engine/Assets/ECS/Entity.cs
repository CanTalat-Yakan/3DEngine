using System.Collections.Generic;
using System.Linq;

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

public sealed partial class Entity
{
    public int ID { get; set; }
    public Guid GUID { get; } = Guid.NewGuid();

    public EntityData Data { get; set; }

    public EntityManager Manager;

    public Transform Transform => Data.Transform;

    public SystemManager SystemManager => _systemManager ??= Kernel.Instance.SystemManager;
    public SystemManager _systemManager;
}

public sealed partial class Entity
{
    public T AddComponent<T>() where T : Component, new() =>
        (T)AddComponent(PoolManager.GetPool<T>().Get());

    public Component AddComponent(Type type)
    {
        var pool = PoolManager.GetPool(type);
        var getMethod = pool.GetType().GetMethod("Get");
        var component = (Component)getMethod.Invoke(pool, null);

        return AddComponent(component);
    }

    private Component AddComponent(Component component)
    {
        component.Entity = this;
        component.IsEnabled = true;

        component.OnRegister();

        if (component is not SimpleComponent)
            if (EditorState.PlayMode || component is EditorComponent)
            {
                // Call the Awake and Start method to initialize the editor component.
                component.OnAwake();
                component.OnStart();
            }

        return component;
    }

    public T[] GetComponent<T>() where T : Component =>
        SystemManager.ComponentManager.GetComponent<T>(this);

    public Component[] GetComponent(Type type) =>
        SystemManager.ComponentManager.GetComponent(this, type);

    public Component[] GetComponents() =>
        SystemManager.ComponentManager.GetComponents(this);

    public Type[] GetComponentTypes() =>
        SystemManager.ComponentManager.GetComponentTypes(this);

    public bool HasComponent<T>() where T : Component =>
        GetComponentTypes().Contains(typeof(T));

    public void RemoveComponent<T>(T component, bool dispose = true) where T : Component
    {
        PoolManager.GetPool<T>().Return(component);

        SystemManager.ComponentManager.RemoveComponent<T>(this, dispose);
    }

    public void RemoveComponent(Component component, bool dispose = true)
    {
        ReturnComponent(component);

        SystemManager.ComponentManager.RemoveComponent(this, component.GetType(), dispose);
    }

    public void RemoveComponents(bool dispose = true)
    {
        foreach (var component in GetComponents())
            ReturnComponent(component);

        SystemManager.ComponentManager.RemoveComponents(this, dispose);
    }

    public void ReturnComponent(Component component)
    {
        var type = component.GetType();
        var pool = PoolManager.GetPool(type);
        var returnMethod = pool.GetType().GetMethod("Return");

        returnMethod.Invoke(pool, [component]);
    }
}

public sealed partial class Entity : ICloneable, IDisposable
{
    object ICloneable.Clone() =>
        Clone();

    public Entity Clone() =>
        Data.Clone();

    public void Dispose()
    {
        Data.Dispose();

        GC.SuppressFinalize(this);
    }

    public void Return() =>
        Data.Return();
}

public sealed partial class EntityData
{
    public Entity Entity;

    public Entity Parent { get => _parent; set => _parent = SetParent(Parent, value); }
    private Entity _parent;

    public List<Entity> Children { get; private set; } = new();

    public string Name;
    public bool IsEnabled = true;
    public bool IsStatic = false;
    [Hide] public bool IsHidden = false;
    public string Tag;
    public string Layer;

    public Transform Transform => _transform ??= Entity.GetComponent<Transform>().FirstOrDefault();
    private Transform _transform;

    public bool ActiveInHierarchy => Parent is null ? IsEnabled : IsEnabled && (Parent.Data.ActiveInHierarchy && Parent.Data.IsEnabled);

    private Entity SetParent(Entity oldParent, Entity newParent)
    {
        if (oldParent != newParent)
        {
            oldParent?.Data.Children.Remove(Entity);
            newParent?.Data.Children.Add(Entity);
        }

        return newParent;
    }

    public string GetDebugInformation()
    {
        var info =
            $"""
            Name: {Name}
            ID: {Entity.ID}
            Scene: {Entity.Manager.Name}
            """;

        if (Parent is not null)
            info +=
                $"""

                Parent: {Parent.Data.Name}
                Parent ID: {Parent.ID}
                """;

        return info;
    }
}

public sealed partial class EntityData : ICloneable, IDisposable
{
    object ICloneable.Clone() =>
        Clone();

    public Entity Clone()
    {
        // Create a new Entity object with a new ID and set its properties.
        EntityData newEntityData = new()
        {
            Name = Name,
            IsEnabled = IsEnabled,
            IsHidden = IsHidden,
            IsStatic = IsStatic,
            Layer = Layer,
            Tag = Tag
        };
        Entity newEntity = Entity.Manager.CreateEntity(newEntityData, Entity.Data.Parent);

        // Copy the original Entity object's Transform properties to the new Entity object.
        newEntity.Transform.LocalPosition = Transform.LocalPosition;
        newEntity.Transform.LocalRotation = Transform.LocalRotation;
        newEntity.Transform.LocalScale = Transform.LocalScale;

        var components = Entity.GetComponents();

        //// Loop through the original Entity object's Components,
        //// clone each one and register it to the new Entity object.
        for (int i = 1; i < components.Count(); i++) // Skip transform.
            newEntity.AddComponent(components[i].GetType()); // TODO: Copy All members

        // Return the new Entity object.
        return newEntity;
    }

    public void Dispose()
    {
        IsEnabled = false;

        Entity.RemoveComponents();

        Parent?.Data.Children.Remove(Entity);

        GC.SuppressFinalize(this);
    }

    public void Return()
    {
        IsEnabled = false;

        Entity.RemoveComponents(dispose: false);

        Parent?.Data.Children.Remove(Entity);
    }
}