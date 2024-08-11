using System.Collections.Generic;

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
    public Dictionary<int, Entity> Entities = new Dictionary<int, Entity>();
    private int nextEntityID = 0;

    public Guid ID = Guid.NewGuid();

    public bool IsEnabled;
    public string Name = "Scene";

    public string LocalPath => $"{localPath}\\{Name}.usda";
    private string localPath = "";

    public Entity Duplicate(Entity refEntity, Entity parent = null)
    {
        Entity clonedEntity = refEntity.Clone();
        clonedEntity.Data.Parent = parent;

        return clonedEntity;
    }

    public void Destroy(EntityData entity) =>
        entity.Dispose();

    public void Dispose()
    {
        foreach (var entity in Entities.Values)
            entity.Dispose();

        Entities.Clear();
    }
}

public sealed partial class EntityManager
{
    public Entity CreateEntity(EntityData data)
    {
        int id = nextEntityID++;
        Entity entity = new(id, data);

        entity.Manager = this;
        data.Entity = entity;

        // Add the Transform component to the Entity when initialized.
        entity.AddComponent<Transform>();

        Entities[id] = entity;

        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        if (Entities.ContainsKey(entity.ID))
            Entities.Remove(entity.ID);
    }

    public Entity GetEntity(int ID) =>
        Entities.TryGetValue(ID, out var entity) ? entity : null;

    public Entity GetEntityFromGUID(Guid guid)
    {
        foreach (var entity in Entities.Values)
            if (entity?.Data.ID == guid)
                return entity;

        return null;
    }

    public Entity GetEntityFromTag(string tag)
    {
        foreach (var entity in Entities.Values)
            if (entity?.Data.Tag == tag)
                return entity;

        return null;
    }

    public IEnumerable<Entity> GetAllEntities() =>
        Entities.Values;
}

public sealed partial class EntityManager
{
    public Entity CreateEntity(Entity parent = null, string name = "New Entity", string tag = "Untagged", bool hide = false)
    {
        EntityData newEntityData = new()
        {
            Name = name,
            Parent = parent,
            Tag = tag,
            IsHidden = hide
        };

        return CreateEntity(newEntityData);
    }

    public Mesh CreatePrimitive(PrimitiveTypes type = PrimitiveTypes.Cube, Entity parent = null, bool hide = false)
    {
        EntityData newEntityData = new()
        {
            Name = type.ToString().FormatString(),
            Parent = parent,
            IsHidden = hide
        };
        Entity newEntity = CreateEntity(newEntityData);

        var mesh = newEntity.AddComponent<Mesh>();
        mesh.SetMeshInfo(ModelLoader.LoadFile(Paths.PRIMITIVES + type.ToString() + ".obj"));
        mesh.SetMaterialTextures(new MaterialTextureEntry("Default.png", 0));
        mesh.SetMaterialPipeline("SimpleLit");

        return mesh;
    }

    public Camera CreateCamera(string name = "Camera", string tag = "Untagged", Entity parent = null, bool hide = false)
    {
        EntityData newEntityData = new()
        {
            Name = name,
            Parent = parent,
            Tag = tag,
            IsHidden = hide
        };
        Entity newEntity = CreateEntity(newEntityData);

        return newEntity.AddComponent<Camera>();
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
