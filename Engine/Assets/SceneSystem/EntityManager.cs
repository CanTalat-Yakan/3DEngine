using System.IO;

namespace Engine.SceneSystem;

public enum PrimitiveTypes
{
    Plane,
    Quad,
    Cube,
    Sphere,
    IcoSphere,
    Cylinder,
    Torus,
    Tree,
    Suzanne,
    Duck,
}

public sealed partial class EntityManager
{
    public EventList<Entity> EntityList = new();

    public Entity Duplicate(Entity refEntity, Entity parent = null)
    {
        // Create a clone of the reference entity.
        Entity clonedEntity = refEntity.Clone();
        // Set the parent of the cloned entity.
        clonedEntity.Parent = parent;

        // Add the cloned entity to the list of entities.
        EntityList.Add(clonedEntity);

        // Return the cloned entity.
        return clonedEntity;
    }

    public void Destroy(Entity entity)
    {
        // Remove the entity from the entity list.
        EntityList.Remove(entity);
        // Call Dispose from IDisposable.
        entity.Dispose();
    }

    public void Dispose()
    {
        foreach (var entity in EntityList)
            entity.Components.Clear();

        EntityList.Clear();
        EntityList = null;

        _materialDefault?.Dispose();
    }
}

public sealed partial class EntityManager
{
    public Entity CreateEntity(Entity parent = null, string name = "New Entity", string tag = "Untagged", bool hide = false)
    {
        Entity newEntity = new()
        {
            Name = name,
            Parent = parent,
            Tag = tag,
            IsHidden = hide
        };

        EntityList.Add(newEntity);

        return newEntity;
    }

    public Mesh CreatePrimitive(PrimitiveTypes type = PrimitiveTypes.Cube, Entity parent = null, bool hide = false)
    {
        // Create a new entity with the specified name and parent.
        Entity newEntity = new()
        {
            Name = type.ToString().FormatString(),
            Parent = parent,
            IsHidden = hide
        };

        // Add a mesh component to the entity using the specified primitive type.
        var mesh = newEntity.AddComponent<Mesh>();
        mesh.SetMeshInfo(Loader.ModelLoader.LoadFile(Path.Combine("Primitives", type.ToString()) + ".obj"));

        // Add the new entity to the entity list.
        EntityList.Add(newEntity);

        // Return the new entity.
        return mesh;
    }

    public Camera CreateCamera(string name = "Camera", string tag = "Untagged", Entity parent = null, bool hide = false)
    {
        // Create a new Entity with the given name, parent, and tag.
        Entity newEntity = new()
        {
            Name = name,
            Parent = parent,
            Tag = tag,
            IsHidden = hide
        };

        // Add a Camera component to the Entity.
        var camera = newEntity.AddComponent<Camera>();

        // Add the new Entity to the EntityList.
        EntityList.Add(newEntity);

        // Return the new Camera.
        return camera;
    }
}

public sealed partial class EntityManager
{
    private static Material _materialDefault;

    public static Material GetDefaultMaterial() =>
        // Create a new material with the default shader and default image.
        _materialDefault ??= new(Paths.SHADERS + "SimpleLit.hlsl");

    public static MeshInfo_OLD GetDefaultMeshInfo() =>
        // Set mesh info to a cube from the resources.
        Loader.ModelLoader.LoadFile(Path.Combine("Primitives", "Cube.obj"));

    public Entity GetFromID(Guid guid)
    {
        // Loop through all entities in the EntityList.
        foreach (var entity in EntityList)
            // Check if the ID of the entity matches the given ID.
            if (entity?.ID == guid)
                // Return the entity if its ID matches the given ID.
                return entity;

        // Return null if the entity is not found.
        return null;
    }

    public Entity GetFromTag(string tag)
    {
        // Iterate over all entities in the EntityList.
        foreach (var entity in EntityList)
            // Check if the tag of the current entity matches the given tag.
            if (entity?.Tag.ToString() == tag)
                // Return the entity if a match is found.
                return entity;

        // Return null if no matching entity is found.
        return null;
    }
}