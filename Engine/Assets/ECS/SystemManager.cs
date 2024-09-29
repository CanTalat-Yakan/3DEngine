using System.Collections.Generic;

namespace Engine.ECS;

public sealed partial class SystemManager
{
    public ComponentManager ComponentManager { get; } = new();

    public EntityManager MainEntityManager;
    public List<EntityManager> SubEntityManagers = new();

    public SystemManager(EntityManager scene = null) =>
        // Initializes the main scene and creates a new empty list for the subscenes.
        MainEntityManager = scene ?? new EntityManager() { Name = "Main Layer", IsEnabled = true };

    public EntityManager AddSubscene(Guid guid = new(), string name = "Sub Layer", bool enable = true)
    {
        EntityManager newSubscene = new() { Name = name, IsEnabled = enable };

        // Set the provided GUID, if not empty.
        if (!guid.Equals(Guid.Empty))
            newSubscene.GUID = guid;

        SubEntityManagers.Add(newSubscene);

        return newSubscene;
    }

    public void LoadSubscene(EntityManager subscene)
    {
        //SceneLoader.Load(subscene);

        SubEntityManagers.Add(subscene);
    }

    public void UnloadSubscene(EntityManager subscene)
    {
        //SceneLoader.Load(subscene);
    }

    public void RemoveSubscene(Guid guid)
    {
        // Retrieves the scene with the specified GUID from the list of subscenes.
        EntityManager entityManager = GetEntityManagerFromGUID(guid);

        // Destroys all entities within the scene.
        entityManager.Dispose();

        SubEntityManagers.Remove(entityManager);
    }
}

public sealed partial class SystemManager
{
    public EntityManager GetEntityManagerFromGUID(Guid guid)
    {
        // Check if the main scene ID matches the provided GUID.
        if (MainEntityManager.GUID == guid)
            return MainEntityManager;

        // Check if any of the subscenes ID matches the provided GUID.
        foreach (var subscene in SubEntityManagers)
            if (subscene.GUID == guid)
                return subscene;

        // Return null if no scene was found with the provided GUID.
        return null;
    }

    public EntityManager GetEntityManagerFromEntityGUID(Guid guid)
    {
        // Check if the main scene contains the entity with an ID that matches the provided GUID.
        if (MainEntityManager.GetEntityFromGUID(guid) is not null)
            return MainEntityManager;

        // Check if any of the subscenes contains the entity with an ID that matches the provided GUID.
        foreach (var subscene in SubEntityManagers)
            if (subscene.GetEntityFromGUID(guid) is not null)
                return subscene;

        // Return null if entity is not found in any scene with the provided GUID.
        return null;
    }
}

public sealed partial class SystemManager
{
    public void ProcessSystems()
    {
        TransformSystem.FetchArray();
        CameraSystem.FetchArray();
        MeshSystem.FetchArray(sort: true);

        ScriptSystem.FetchArray();
        EditorSystem.FetchArray();
        SimpleSystem.FetchArray();
    }

    public void Awake()
    {
        TransformSystem.Awake();
        CameraSystem.Awake();

        if (EditorState.PlayMode)
            ScriptSystem.Awake();
    }

    public void Start()
    {
        if (EditorState.PlayMode)
            ScriptSystem.Start();
    }

    public void Update()
    {
        MeshSystem.Update();

        EditorSystem.Update();

        if (EditorState.PlayMode)
            ScriptSystem.Update();

        SimpleSystem.SimpleUpdate();
    }

    public void LateUpdate()
    {
        EditorSystem.LateUpdate();

        if (EditorState.PlayMode)
            ScriptSystem.LateUpdate();
    }

    public void FixedUpdate()
    {
        EditorSystem.FixedUpdate();

        if (EditorState.PlayMode)
            ScriptSystem.FixedUpdate();
    }

    public void Render()
    {
        CameraSystem.Render();

        MeshSystem.Render();

        Mesh.CurrentMeshDataOnGPU = null;
        Mesh.CurrentMaterialOnGPU = null;
    }

    public void GUI()
    {
        EditorSystem.GUI();
        ScriptSystem.GUI();
    }

    public void Dispose()
    {
        CameraSystem.Destroy();
        MeshSystem.Destroy();

        MainEntityManager.Dispose();
        foreach (var scene in SubEntityManagers)
            scene.Dispose();
    }
}