using System.Collections.Generic;

namespace Engine.ECS;

internal enum EEditorTags
{
    SceneBoot,
    SceneCamera,
    SceneSky,
}

internal enum ETags
{
    Untagged,
    MainCamera,
    Respawn,
    Player,
    Finish,
    GameController,
}

internal enum ELayers
{
    Default,
    TransparentFX,
    IgnoreRaycast,
    Water,
    UI
}

public class Entity : ICloneable
{
    public Guid ID = Guid.NewGuid();

    public Entity Parent;

    public string Name;
    public bool IsEnabled = true;
    public bool IsStatic = false;
    public string Tag;
    public string Layer;

    public Scene Scene { get => _scene is null ? _scene = SceneManager.GetFromEntityID(ID) : _scene; set => _scene = null; }
    private Scene _scene;

    public Transform Transform => _transform;
    private Transform _transform;

    public bool ActiveInHierarchy => Parent is null ? IsEnabled : IsEnabled && (Parent.ActiveInHierarchy && Parent.IsEnabled);

    private List<Component> _components = new();

    public Entity() =>
        // Add the Transform component to the Entity when initialized.
        AddComponent(_transform = new Transform());

    public void AddComponent<T>() where T : Component, new() =>
        AddComponent(new T());

    public void AddComponent(Type type) =>
        AddComponent((Component)Activator.CreateInstance(type));

    public void AddComponent(Component component)
    {
        // Add the component to the Entity's component list.
        _components.Add(component);
        // Assign this Entity to the component's Entity.
        component.Entity = this;
        // Enable the component by default.
        component.IsEnabled = true;
    }

    public void RemoveComponent(Component component)
    {
        // Remove the component from the Entity's component list.
        _components.Remove(component);
        // Invoke the component's OnDestroy event.
        component.InvokeEventOnDestroy();
    }

    public void RemoveComponents()
    {
        // Destroy all components associated with the Entity.
        foreach (var component in _components)
            component.InvokeEventOnDestroy();

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

    public bool CompareTag(params string[] tags)
    {
        // Check if the Entity has a specified tag.
        foreach (var tag in tags)
            // If the Entity has the tag, return true.
            if (Tag == tag)
                return true;

        // Return false if no matching tag is found.
        return false;
    }

    public Component[] GetComponents() =>
        _components.ToArray();

    object ICloneable.Clone() =>
        Clone();

    public Entity Clone()
    {
        //var newEntity = (Entity)MemberwiseClone();
        //newEntity.ID = Guid.NewGuid();

        // Create a new Entity object with a new ID and set its properties.
        var newEntity = new Entity()
        {
            ID = Guid.NewGuid(), // Generate a new ID for the new Entity object.
            Name = Name,
            IsEnabled = IsEnabled,
            IsStatic = IsStatic,
            Layer = Layer,
            Tag = Tag,
        };

        // Copy the original Entity object's Transform properties to the new Entity object.
        newEntity.Transform.LocalPosition = Transform.LocalPosition;
        newEntity.Transform.LocalRotation = Transform.LocalRotation;
        newEntity.Transform.LocalScale = Transform.LocalScale;

        // Loop through the original Entity object's Components, clone each one and register it to the new Entity object.
        for (int i = 1; i < _components.Count; i++)
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
