using System.Collections.Generic;

namespace Engine.ECS;

public sealed partial class EntityManager
{
    public EventDictionary<int, Entity> Entities = new();
    private int nextEntityID = 0;

    public Guid GUID = Guid.NewGuid();

    public bool IsEnabled;
    public string Name = "Scene";

    public string LocalPath => $"{localPath}\\{Name}.usda";
    private string localPath = "";

    public void Dispose()
    {
        foreach (var entity in Entities.Values)
            entity.Dispose();

        Entities.Clear();

        GC.SuppressFinalize(this);
    }
}

public sealed partial class EntityManager
{
    public Entity CreateEntity(EntityData data, Entity parent = null)
    {
        Entity entity = PoolManager.GetPool<Entity>().Get();
        entity.ID = nextEntityID++;
        entity.Data = data;

        entity.Manager = this;
        data.Entity = entity;
        data.Parent = parent;

        // Add the Transform component to the Entity when initialized.
        entity.AddComponent<Transform>();

        Entities[entity.ID] = entity;

        return entity;
    }

    public Entity DuplicateEntity(Entity refEntity, Entity parent = null)
    {
        Entity clonedEntity = refEntity.Clone();
        clonedEntity.Data.Parent = parent;

        return clonedEntity;
    }

    public Entity MigrateEntity(Entity entity, Entity parent = null)
    {
        int id = nextEntityID++;

        entity.Manager.Entities.Remove(entity.ID);
        entity.Manager = this;

        entity.ID = id;
        entity.Data.Parent = parent;

        Entities[id] = entity;

        return entity;
    }

    public void DestroyEntity(Entity entity)
    {
        if (entity is null)
            return;

        if (!Entities.ContainsKey(entity.ID))
            return;

        entity.Dispose();

        Entities.Remove(entity.ID);
    }

    public void ReturnEntity(Entity entity)
    {
        if (entity is null)
            return;

        if (!Entities.ContainsKey(entity.ID))
            return;

        entity.Return();

        Entities.Remove(entity.ID);

        PoolManager.GetPool<Entity>().Return(entity);
    }

    public Entity GetEntity(int ID) =>
        Entities.TryGetValue(ID, out var entity) ? entity : null;

    public Entity GetEntityFromGUID(Guid guid)
    {
        foreach (var entity in Entities.Values)
            if (entity?.GUID == guid)
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
            Tag = tag,
            IsHidden = hide
        };

        return CreateEntity(newEntityData, parent);
    }

    public Mesh CreatePrimitive(PrimitiveFiles type = PrimitiveFiles.Cube, Entity parent = null, bool hide = false)
    {
        EntityData newEntityData = new()
        {
            Name = type.ToString().FormatString(),
            IsHidden = hide
        };
        Entity newEntity = CreateEntity(newEntityData, parent);

        var mesh = newEntity.AddComponent<Mesh>();
        mesh.SetMeshData(ModelLoader.LoadFile(AssetPaths.PRIMITIVES + type.ToString() + ".obj"));
        mesh.SetMaterialTextures(new MaterialTextureEntry("Default.png", 0));
        mesh.SetRootSignature();
        mesh.SetMaterialPipeline("SimpleLit");

        return mesh;
    }

    public Camera CreateCamera(string name = "Camera", string tag = "Untagged", Entity parent = null, bool hide = false)
    {
        EntityData newEntityData = new()
        {
            Name = name,
            Tag = tag,
            IsHidden = hide
        };
        Entity newEntity = CreateEntity(newEntityData, parent);

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
        newEntityManager.GUID = Guid.NewGuid();

        return newEntityManager;
    }
}