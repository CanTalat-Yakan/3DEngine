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
        // Adds a new scene with the specified parameters to the list of subscenes.
        Scene subscene = new()
        {
            Name = name,
            IsEnabled = enable,
            EntityManager = new()
        };
        // Set the provided GUID, if not empty.
        if (!guid.Equals(Guid.Empty))
            subscene.ID = guid;

        Subscenes.Add(subscene);

        // Returns the newly added scene.
        return subscene;
    }

    public static void LoadSubscene(Scene subscene)
    {
        // Calls the load method on the specified subscene.
        subscene.Load();

        // Adds the loaded subscene to the list of subscenes.
        Subscenes.Add(subscene);
    }

    public static void UnloadSubscene(Scene subscene)
    {
        // Calls the unload method on the specified subscene.
        subscene.Unload();
    }

    public static void RemoveSubscene(Guid guid)
    {
        // Retrieves the scene with the specified GUID from the list of subscenes.
        Scene scene = GetFromID(guid);

        // Destroys all entities within the scene.
        foreach (var entity in scene.EntityManager.EntityList.ToArray())
            scene.EntityManager.Destroy(entity);

        // Removes the scene from the list of subscenes.
        Subscenes.Remove(scene);
    }

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
        // Copy TransformSystem to Array.
        TransformSystem.CopyToArray();
        // Sort and Copy CameraSystem to Array.
        CameraSystem.CopyToArray();
        // Sort and Copy MeshSystem to Array if it has changes.
        MeshSystem.SortAndCopyToArrayIfDirty();
        // Copy EditorScriptSystem to Array.
        EditorScriptSystem.CopyToArray();
        // Copy ScriptSystem to Array.
        ScriptSystem.CopyToArray();
    }

    public void Awake()
    {
        // Awake the TransformSystem.
        TransformSystem.Awake();
        // Awake the CameraSystem.
        CameraSystem.Awake();

        if (EditorState.PlayMode)
            // Awake the ScriptSystem.
            ScriptSystem.Awake();
    }

    public void Start()
    {
        if (EditorState.PlayMode)
            // Start the ScriptSystem.
            ScriptSystem.Start();
    }

    public void Update()
    {
        // Update the TransformSystem.
        Profiler.Benchmark(TransformSystem.Update, "Transform Update");
        // Update the CameraSystem
        CameraSystem.Update();
        // Update the MeshSystem.
        MeshSystem.Update();

        // Update the EditorScriptSystem.
        EditorScriptSystem.Update();

        if (EditorState.PlayMode)
            // Update the ScriptSystem.
            ScriptSystem.Update();
    }

    public void LateUpdate()
    {
        // LateUpdate the EditorScriptSystem.
        EditorScriptSystem.LateUpdate();

        if (EditorState.PlayMode)
            // LateUpdate the ScriptSystem.
            ScriptSystem.LateUpdate();
    }

    public void FixedUpdate()
    {
        // FixedUpdate the EditorScriptSystem.
        EditorScriptSystem.FixedUpdate();

        if (EditorState.PlayMode)
            // FixedUpdate the ScriptSystem.
            ScriptSystem.FixedUpdate();
    }

    public void Render()
    {
        // Call the Render in ScriptSystem.
        ScriptSystem.Render();
        // Call the Render in EditorScriptSystem.
        EditorScriptSystem.Render();

        // Render the MeshSystem.
        Profiler.Benchmark(MeshSystem.Render, "Mesh Render");

        // Call the Render in TransformSystem.
        TransformSystem.Render();

        Mesh.CurrentMeshOnGPU = null;
        Material.CurrentMaterialOnGPU = null;
    }

    public void GUI()
    {
        // Render the GUI for the EditorScriptSystem.
        EditorScriptSystem.GUI();
        // Render the GUI for the ScriptSystem.
        ScriptSystem.GUI();
    }

    public void Dispose()
    {
        // Dispose all Systems.
        CameraSystem.Dispose();
        MeshSystem.Dispose();

        MainScene.EntityManager.Dispose();
        foreach (var scene in Subscenes)
            scene.EntityManager.Dispose();
    }
}