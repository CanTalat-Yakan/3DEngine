using System.Collections.Generic;
using System;
using Engine.ECS;
using Editor.Controller;
using Engine.Components;

namespace Engine.Utilities
{
    internal class SceneManager
    {
        public static Scene Scene;
        public static List<Scene> Subscenes;

        public SceneManager(Scene scene = null)
        {
            Scene = scene != null ? scene : new() { ID = Guid.NewGuid(), Name = "Core", IsEnabled = true, EntitytManager = new() };
            Subscenes = new List<Scene>();
        }

        public static Scene AddSubscene(Guid guid = new(), string name = "Subscene", bool enable = true)
        {
            if (guid.Equals(Guid.Empty))
                guid = Guid.NewGuid();

            Scene scene;
            Subscenes.Add(scene = new() { ID = guid, Name = name, IsEnabled = enable, EntitytManager = new() });

            return scene;
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
            Scene scene = GetFromID(guid);

            foreach (var entity in scene.EntitytManager.EntityList)
                scene.EntitytManager.Destroy(entity);

            Subscenes.Remove(scene);
        }

        public void Awake()
        {
            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.None)
                EditorScriptSystem.Awake();

            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.Playing)
                ScriptSystem.Awake();
        }

        public void Start()
        {
            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.None)
                EditorScriptSystem.Start();

            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.Playing)
                ScriptSystem.Start();
        }

        public void Update()
        {
            TransformSystem.Update();

            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.None)
                EditorScriptSystem.Update();

            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.Playing)
                ScriptSystem.Update();
        }

        public void LateUpdate()
        {
            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.None)
                EditorScriptSystem.LateUpdate();

            if (Main.Instance.ControlPlayer.Playmode == EPlaymode.Playing)
                ScriptSystem.LateUpdate();
        }

        public void Render()
        {
            TransformSystem.Update();
            CameraSystem.Sort();
            CameraSystem.Render();
            MeshSystem.Render();
        }

        public static Scene GetFromID(Guid guid)
        {
            if (Scene.ID == guid)
                return Scene;

            foreach (var subscene in Subscenes)
                if (subscene.ID == guid)
                    return subscene;

            return null;
        }

        public static Scene GetFromEntityID(Guid guid)
        {
            if (Scene.EntitytManager.GetFromID(guid) != null)
                return Scene;

            foreach (var subscene in Subscenes)
                if (subscene.EntitytManager.GetFromID(guid) != null)
                    return subscene;

            return null;
        }
    }
}
