using System.Collections.Generic;

namespace Engine.ECS;

public sealed partial class SystemManager
{
    public EntityManager MainScene;
    public List<EntityManager> SubScenes = new();

    public SystemManager(EntityManager scene = null) =>
        // Initializes the main scene and creates a new empty list for the subscenes.
        MainScene = scene ?? new EntityManager() { Name = "Main", IsEnabled = true };

    public EntityManager AddSubscene(Guid guid = new(), string name = "Subscene", bool enable = true)
    {
        EntityManager newSubscene = new() { Name = name, IsEnabled = enable };

        // Set the provided GUID, if not empty.
        if (!guid.Equals(Guid.Empty))
            newSubscene.ID = guid;

        SubScenes.Add(newSubscene);

        return newSubscene;
    }

    public void LoadSubscene(EntityManager subscene)
    {
        //SceneLoader.Load(subscene);

        SubScenes.Add(subscene);
    }

    public void UnloadSubscene(EntityManager subscene)
    {
        //SceneLoader.Load(subscene);
    }

    public void RemoveSubscene(Guid guid)
    {
        var entityManager = GetFromID(guid);

        entityManager.Dispose();

        SubScenes.Remove(entityManager);
    }
}

public sealed partial class SystemManager
{
    public EntityManager GetFromID(Guid guid)
    {
        // Check if the main scene ID matches the provided GUID.
        if (MainScene.ID == guid)
            return MainScene;

        // Check if any of the subscenes ID matches the provided GUID.
        foreach (var subscene in SubScenes)
            if (subscene.ID == guid)
                return subscene;

        // Return null if no scene was found with the provided GUID.
        return null;
    }

    public EntityManager GetFromEntityID(Guid guid)
    {
        // Check if the main scene contains the entity with an ID that matches the provided GUID.
        if (MainScene.GetFromID(guid) is not null)
            return MainScene;

        // Check if any of the subscenes contains the entity with an ID that matches the provided GUID.
        foreach (var subscene in SubScenes)
            if (subscene.GetFromID(guid) is not null)
                return subscene;

        // Return null if entity is not found in any scene with the provided GUID.
        return null;
    }
}

public sealed partial class SystemManager
{
    public void ProcessSystems()
    {
        TransformSystem.CachePoolIfDirty();
        CameraSystem.CachePoolIfDirty();
        MeshSystem.CachePoolIfDirty();
        EditorScriptSystem.CachePoolIfDirty();
        ScriptSystem.CachePoolIfDirty();
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
        Profiler.Benchmark("Transform Update",
            TransformSystem.Update);
        MeshSystem.Update();

        EditorScriptSystem.Update();

        if (EditorState.PlayMode)
            ScriptSystem.Update();
    }

    public void LateUpdate()
    {
        EditorScriptSystem.LateUpdate();

        if (EditorState.PlayMode)
            ScriptSystem.LateUpdate();
    }

    public void FixedUpdate()
    {
        EditorScriptSystem.FixedUpdate();

        if (EditorState.PlayMode)
            ScriptSystem.FixedUpdate();
    }

    public void Render()
    {
        CameraSystem.Render();

        ScriptSystem.Render();
        EditorScriptSystem.Render();

        Profiler.Benchmark("Mesh Render",
            MeshSystem.Render);

        TransformSystem.Render();

        Mesh.CurrentMeshInfoOnGPU = null;
        Mesh.CurrentMaterialOnGPU = null;
    }

    public void GUI()
    {
        EditorScriptSystem.GUI();
        ScriptSystem.GUI();
    }

    public void Dispose()
    {
        CameraSystem.Destroy();
        MeshSystem.Destroy();

        MainScene.Dispose();
        foreach (var scene in SubScenes)
            scene.Dispose();
    }
}