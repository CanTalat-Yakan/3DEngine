using System.Collections.Generic;

namespace Engine.SceneSystem;

public sealed partial class SceneManager
{
    public static Scene MainScene;
    public static List<Scene> Subscenes = new();

    public SceneManager(Scene scene = null) =>
        // Initializes the main scene and creates a new empty list for the subscenes.
        MainScene = scene ?? new Scene() { Name = "Main", IsEnabled = true, EntityManager = new() };

    public static Scene AddSubscene(Guid guid = new(), string name = "Subscene", bool enable = true)
    {
        Scene newSubscene = new()
        {
            Name = name,
            IsEnabled = enable,
            EntityManager = new()
        };

        // Set the provided GUID, if not empty.
        if (!guid.Equals(Guid.Empty))
            newSubscene.ID = guid;

        Subscenes.Add(newSubscene);

        return newSubscene;
    }

    public static void LoadSubscene(Scene subscene)
    {
        subscene.Load();

        Subscenes.Add(subscene);
    }

    public static void UnloadSubscene(Scene subscene)
    {
        subscene.Unload();
    }

    public static void RemoveSubscene(Guid guid)
    {
        // Retrieves the scene with the specified GUID from the list of subscenes.
        Scene scene = GetFromID(guid);

        // Destroys all entities within the scene.
        foreach (var entity in scene.EntityManager.EntityList.ToArray())
            scene.EntityManager.Destroy(entity);

        Subscenes.Remove(scene);
    }
}

public sealed partial class SceneManager
{
    public static Scene GetFromID(Guid guid)
    {
        // Check if the main scene ID matches the provided GUID.
        if (MainScene.ID == guid)
            return MainScene;

        // Check if any of the subscenes ID matches the provided GUID.
        foreach (var subscene in Subscenes)
            if (subscene.ID == guid)
                return subscene;

        // Return null if no scene was found with the provided GUID.
        return null;
    }

    public static Scene GetFromEntityID(Guid guid)
    {
        // Check if the main scene contains the entity with an ID that matches the provided GUID.
        if (MainScene.EntityManager.GetFromID(guid) is not null)
            return MainScene;

        // Check if any of the subscenes contains the entity with an ID that matches the provided GUID.
        foreach (var subscene in Subscenes)
            if (subscene.EntityManager.GetFromID(guid) is not null)
                return subscene;

        // Return null if entity is not found in any scene with the provided GUID.
        return null;
    }
}

public sealed partial class SceneManager
{
    public void ProcessSystems()
    {
        TransformSystem.CopyToArray();
        CameraSystem.CopyToArray();
        MeshSystem.SortAndCopyToArrayIfDirty();
        EditorScriptSystem.CopyToArray();
        ScriptSystem.CopyToArray();
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

        Mesh.OnGPU = null;
        Material.OnGPU = null;
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

        MainScene.EntityManager.Dispose();
        foreach (var scene in Subscenes)
            scene.EntityManager.Dispose();
    }
}