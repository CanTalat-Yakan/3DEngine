using System.Collections.Generic;
using System;
using Editor.Controller;
using Engine.ECS;

namespace Engine.Utilities;

internal class SceneManager
{
    public static Scene Scene;
    public static List<Scene> Subscenes;

    public SceneManager(Scene scene = null)
    {
        // Initializes the main scene and creates a new empty list for the subscenes.
        Scene = scene != null ? scene : new() { ID = Guid.NewGuid(), Name = "Core", IsEnabled = true, EntitytManager = new() };
        Subscenes = new List<Scene>();
    }

    public static Scene AddSubscene(Guid guid = new(), string name = "Subscene", bool enable = true)
    {
        // If the provided GUID is empty, a new one is generated.
        if (guid.Equals(Guid.Empty))
            guid = Guid.NewGuid();

        // Adds a new scene with the specified parameters to the list of subscenes.
        Scene scene;
        Subscenes.Add(scene = new() { ID = guid, Name = name, IsEnabled = enable, EntitytManager = new() });

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
        foreach (var entity in scene.EntitytManager.EntityList)
            scene.EntitytManager.Destroy(entity);

        // Removes the scene from the list of subscenes.
        Subscenes.Remove(scene);
    }

    public void Awake()
    {
        // Update the CameraSystem
        CameraSystem.Awake();

        // If the playmode is set to None and is not running,
        // call the Awake method on the EditorScriptSystem.
        if (Main.Instance.PlayerControl.PlayMode == EPlayMode.None)
            EditorScriptSystem.Awake();

        // If the playmode is set to Playing and is not paused,
        // call the Awake method on the ScriptSystem.
        if (Main.Instance.PlayerControl.PlayMode == EPlayMode.Playing)
            ScriptSystem.Awake();
    }

    public void Start()
    {
        // If the playmode is set to None and is not running,
        // call the Start method on the EditorScriptSystem.
        if (Main.Instance.PlayerControl.PlayMode == EPlayMode.None)
            EditorScriptSystem.Start();

        // If the playmode is set to Playing and is not paused,
        // call the Start method on the ScriptSystem.
        if (Main.Instance.PlayerControl.PlayMode == EPlayMode.Playing)
            ScriptSystem.Start();
    }

    public void Update()
    {
        // Update the TransformSystem
        TransformSystem.Update();
        // Update the CameraSystem
        CameraSystem.Update();

        // If the playmode is set to None and is not running,
        // call the Update method on the EditorScriptSystem.
        if (Main.Instance.PlayerControl.PlayMode == EPlayMode.None)
            EditorScriptSystem.Update();

        // If the playmode is set to Playing and is not paused,
        // call the Update method on the ScriptSystem.
        if (Main.Instance.PlayerControl.PlayMode == EPlayMode.Playing)
            ScriptSystem.Update();
    }

    public void LateUpdate()
    {
        // If the playmode is set to None and is not running,
        // call the LateUpdate method on the EditorScriptSystem.
        if (Main.Instance.PlayerControl.PlayMode == EPlayMode.None)
            EditorScriptSystem.LateUpdate();

        // If the playmode is set to Playing and is not paused,
        // call the LateUpdate method on the ScriptSystem.
        if (Main.Instance.PlayerControl.PlayMode == EPlayMode.Playing)
            ScriptSystem.LateUpdate();
    }

    public void Render()
    {
        // Update the TransforSystem.
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
        if (Scene.ID == guid)
            return Scene;

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
        if (Scene.EntitytManager.GetFromID(guid) != null)
            return Scene;

        // Check if any of the subscenes contains the entity with an ID that matches the provided GUID.
        foreach (var subscene in Subscenes)
            if (subscene.EntitytManager.GetFromID(guid) != null)
                return subscene;

        // Return null if entity is not found in any scene with the provided GUID.
        return null;
    }
}
