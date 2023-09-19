using System.Collections.Generic;

namespace Engine.Utilities;

public sealed class SceneManager
{
    public static Scene MainScene;
    public static List<Scene> Subscenes;

    public SceneManager(Scene scene = null)
    {
        // Initializes the main scene and creates a new empty list for the subscenes.
        MainScene = scene != null ? scene : new() { ID = Guid.NewGuid(), Name = "Main", IsEnabled = true, EntityManager = new() };
        Subscenes = new List<Scene>();
    }

    public static Scene AddSubscene(Guid guid = new(), string name = "Subscene", bool enable = true)
    {
        // If the provided GUID is empty, a new one is generated.
        if (guid.Equals(Guid.Empty))
            guid = Guid.NewGuid();

        // Adds a new scene with the specified parameters to the list of subscenes.
        Scene scene = new()
        {
            ID = guid,
            Name = name,
            IsEnabled = enable,
            EntityManager = new()
        };
        Subscenes.Add(scene);

        // Returns the newly added scene.
        return scene;
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

        // Removes the specified subscene from the list of subscenes.
        Subscenes.Remove(subscene);
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

    public void Awake()
    {
        // Update the CameraSystem
        CameraSystem.Awake();

        if (!Core.PlayMode)
            // Awake the EditorScriptSystem.
            EditorScriptSystem.Awake();

        if (Core.PlayMode)
            // Awake the ScriptSystem.
            ScriptSystem.Awake();
    }

    public void Start()
    {
        if (!Core.PlayMode)
            // Start the EditorScriptSystem.
            EditorScriptSystem.Start();

        if (Core.PlayMode)
            // Start the ScriptSystem.
            ScriptSystem.Start();
    }

    public void Update()
    {
        // Render the MeshSystem.
        MeshSystem.Update();
        // Update the TransformSystem
        TransformSystem.Update();
        // Update the CameraSystem
        CameraSystem.Update();

        if (!Core.PlayMode)
            // Update the EditorScriptSystem.
            EditorScriptSystem.Update();

        if (Core.PlayMode)
            // Update the ScriptSystem.
            ScriptSystem.Update();
    }

    public void LateUpdate()
    {
        if (!Core.PlayMode)
            // LateUpdate the EditorScriptSystem.
            EditorScriptSystem.LateUpdate();

        if (Core.PlayMode)
            // LateUpdate the ScriptSystem.
            ScriptSystem.LateUpdate();
    }

    public void FixedUpdate()
    {
        if (!Core.PlayMode)
            // FixedUpdate the EditorScriptSystem.
            EditorScriptSystem.FixedUpdate();

        if (Core.PlayMode)
            // FixedUpdate the ScriptSystem.
            ScriptSystem.FixedUpdate();
    }

    public void Render()
    {
        // Update the TransformSystem.
        TransformSystem.Update();

        // Sort and Render the Cameras.
        CameraSystem.Sort();
        CameraSystem.Render();

        // Render the MeshSystem.
        MeshSystem.Render();
    }

    public static Scene GetFromID(Guid guid)
    {
        // Check if the main scene's ID matches the provided GUID.
        if (MainScene.ID == guid)
            return MainScene;

        // Check if any of the subscenes' ID matches the provided GUID.
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
