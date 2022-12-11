using System.Collections.Generic;
using System;
using Engine.ECS;
using Editor.Controller;

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

        public static void LoadScene(Scene scene)
        {
            Scene = scene;
            Subscenes = new List<Scene>();
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

        public static void RemoveScene(Guid guid)
        {
            Subscenes.Remove(GetFromID(guid));
        }

        public void Awake()
        {
            TransformSystem.Awake();
            CameraSystem.Awake();
            MeshSystem.Awake();
            EditorScriptSystem.Awake();

            if (Main.Instance.ControlPlayer.PlayMode == EPlayMode.Playing)
                ScriptSystem.Awake();
        }

        public void Start()
        {
            TransformSystem.Start();
            CameraSystem.Start();
            MeshSystem.Start();
            EditorScriptSystem.Start();

            if (Main.Instance.ControlPlayer.PlayMode == EPlayMode.Playing)
                ScriptSystem.Start();
        }

        public void Update()
        {
            TransformSystem.Update();
            CameraSystem.Update();
            MeshSystem.Update();
            EditorScriptSystem.Update();

            if (Main.Instance.ControlPlayer.PlayMode == EPlayMode.Playing)
                ScriptSystem.Update();
        }

        public void LateUpdate()
        {
            TransformSystem.LateUpdate();
            CameraSystem.LateUpdate();
            MeshSystem.LateUpdate();
            EditorScriptSystem.LateUpdate();

            if (Main.Instance.ControlPlayer.PlayMode == EPlayMode.Playing)
                ScriptSystem.LateUpdate();
        }

        public void Render()
        {
            MeshSystem.Render();
            EditorScriptSystem.Render();
        }

        public string Profile()
        {
            return Scene.Profile();
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
    }
}
