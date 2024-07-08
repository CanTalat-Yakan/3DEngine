using EnTTSharp.Entities;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using static Assimp.Metadata;

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
    public readonly EntityRegistry<EntityKey> Registry;

    public Guid ID = Guid.NewGuid();

    public bool IsEnabled;
    public string Name = "Scene";

    public string LocalPath => $"{localPath}\\{Name}.usda";
    private string localPath = "";

    public EntityManager()
    {
        // Define the entity key factory function
        Func<byte, int, EntityKey> entityKeyFactory = (generation, index) => new EntityKey(generation, index);
        // Instantiate the registry with the desired maxAge(reuse limit) and the entityKeyFactory function
        Registry = new(10, entityKeyFactory);

        Registry.Register<Component>();
        Registry.Register<EditorComponent>();
    }

    public EntityKey Duplicate(EntityKey refEntity, EntityKey? parentKey = null)
    {
        var newEntity = Registry.Create();

        var newEntityData = GetEntityData(refEntity).Clone();

        if (parentKey is not null)
            newEntityData.Parent = GetEntityData(parentKey.Value);

        Registry.AssignComponent(newEntity, newEntityData);

        return newEntity;
    }

    public void Destroy(EntityKey entity)
    {
        if (Registry.Contains(entity))
        {
            GetEntityData(entity).OnDestroy();

            Registry.Destroy(entity);
        }
    }

    public void Dispose()
    {
        foreach (var entity in Registry)
        {
            GetEntityData(entity).OnDestroy();

            Registry.Destroy(entity);
        }
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
    public EntityData CreateEntity(EntityData parent = null, string name = "New Entity", string tag = "Untagged", bool hide = false)
    {
        var newEntityKey = Registry.Create();

        EntityData newEntityData = new(this, newEntityKey)
        {
            Name = name,
            Parent = parent,
            Tag = tag,
            IsHidden = hide
        };

        return newEntityData;
    }

    public Mesh CreatePrimitive(PrimitiveTypes type = PrimitiveTypes.Cube, EntityData parent = null, bool hide = false)
    {
        var newEntityKey = Registry.Create();

        EntityData newEntityData = new(this, newEntityKey)
        {
            Name = type.ToString().FormatString(),
            Parent = parent,
            IsHidden = hide
        };

        var mesh = newEntityData.AddComponent<Mesh>();
        mesh.SetMeshInfo(ModelLoader.LoadFile(Paths.PRIMITIVES + type.ToString() + ".obj"));
        mesh.SetMaterialTextures(new MaterialTextureEntry("Default.png", 0));
        mesh.SetMaterialPipeline("SimpleLit");

        return mesh;
    }

    public Camera CreateCamera(string name = "Camera", string tag = "Untagged", EntityData parent = null, bool hide = false)
    {
        var newEntityKey = Registry.Create();

        EntityData newEntityData = new(this, newEntityKey)
        {
            Name = name,
            Parent = parent,
            Tag = tag,
            IsHidden = hide
        };

        var camera = newEntityData.AddComponent<Camera>();

        return camera;
    }
}

public sealed partial class EntityManager
{
    public EntityData[] GetAllEntityData()
    {
        List<EntityData> entityDataList = new();

        foreach (var entity in Registry)
            entityDataList.Add(GetEntityData(entity));

        return entityDataList.ToArray();
    }

    public static EntityData GetEntityData(EntityKey entity)
    {
        if (EntityDataSystem.ComponentPool.TryGet(entity, out var entityData))
            return entityData;
        else
            return null;
    }

    public EntityData GetFromID(Guid guid)
    {
        EntityData entityData = null;

        foreach (var entity in Registry)
        {
            entityData = GetEntityData(entity);

            if (entityData?.ID == guid)
                return entityData;
        }

        return null;
    }

    public EntityData GetFromTag(string tag)
    {

        EntityData entityData = null;

        foreach (var entity in Registry)
        {
            entityData = GetEntityData(entity);

            if (entityData?.Tag == tag)
                return entityData;
        }

        return null;
    }
}