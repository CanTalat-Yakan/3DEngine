using System.Collections.Generic;
using System;
using Engine.ECS;

namespace Engine.Utilities
{
    internal class SceneManager
    {
        public static Scene Scene;
        public static List<Scene> Subscenes;

        public SceneManager(Scene scene = null)
        {
            Scene = scene != null ? scene : new Scene() { ID = new Guid(), Name = "Core", IsEnabled = true, EntitytManager = new EntityManager() };
            Subscenes = new List<Scene>();
        }

        public static Scene AddSubscene(Guid guid = new Guid(), string name = "Subscene", bool enable = true)
        {
            if (guid.Equals(Guid.Empty))
                guid = Guid.NewGuid();

            Scene scene;
            Subscenes.Add(scene = new Scene() { ID = guid, Name = name, IsEnabled = enable, EntitytManager = new EntityManager() });

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
            ScriptSystem.Awake();
        }

        public void Start()
        {
            TransformSystem.Start();
            CameraSystem.Start();
            MeshSystem.Start();
            ScriptSystem.Start();
        }

        public void Update()
        {
            TransformSystem.Update();
            CameraSystem.Update();
            MeshSystem.Update();
            ScriptSystem.Update();
        }

        public void LateUpdate()
        {
            TransformSystem.LateUpdate();
            CameraSystem.LateUpdate();
            MeshSystem.LateUpdate();
            ScriptSystem.LateUpdate();
        }

        public void Render()
        {
            MeshSystem.Render();
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
