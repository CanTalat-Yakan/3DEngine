using Engine.Loader;

namespace Engine.ECS;

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
    public Guid ID = Guid.NewGuid();

    public bool IsEnabled;
    public string Name = "Scene";

    public string LocalPath => $"{localPath}\\{Name}.usda";
    private string localPath = "";

    public Entity Duplicate(Entity refEntity, Entity parent = null)
    {
        Entity clonedEntity = refEntity.Clone();
        clonedEntity.Parent = parent;

        EntityList.Add(clonedEntity);

        return clonedEntity;
    }

    public void Destroy(Entity entity)
    {
        EntityList.Remove(entity);
        entity.Dispose();
    }

    public void Dispose()
    {
        foreach (var entity in EntityList)
            entity.Components.Clear();

        EntityList.Clear();
        EntityList = null;
    }
}

public sealed partial class EntityManager : ICloneable
{

    object ICloneable.Clone() =>
        Clone();

    public EntityManager Clone()
    {
        var newEntityManager = (EntityManager)this.MemberwiseClone();
        newEntityManager.ID = Guid.NewGuid();

        return newEntityManager;
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
        Entity newEntity = new()
        {
            Name = type.ToString().FormatString(),
            Parent = parent,
            IsHidden = hide
        };

        var mesh = newEntity.AddComponent<Mesh>();
        mesh.SetMeshInfo(ModelLoader.LoadFile(Paths.PRIMITIVES + type.ToString() + ".obj"));
        mesh.SetMaterialTextures(new MaterialTextureEntry("Default.png", 0));
        mesh.SetMaterialPipeline("SimpleLit");

        EntityList.Add(newEntity);

        return mesh;
    }

    public Camera CreateCamera(string name = "Camera", string tag = "Untagged", Entity parent = null, bool hide = false)
    {
        Entity newEntity = new()
        {
            Name = name,
            Parent = parent,
            Tag = tag,
            IsHidden = hide
        };

        var camera = newEntity.AddComponent<Camera>();

        EntityList.Add(newEntity);

        return camera;
    }
}

public sealed partial class EntityManager
{
    public Entity GetFromID(Guid guid)
    {
        foreach (var entity in EntityList)
            if (entity?.ID == guid)
                return entity;

        return null;
    }

    public Entity GetFromTag(string tag)
    {
        foreach (var entity in EntityList)
            if (entity?.Tag == tag)
                return entity;

        return null;
    }
}