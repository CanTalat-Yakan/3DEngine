using Editor.Controller;
using Microsoft.UI.Composition.Scenes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

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
            if(guid.Equals(Guid.Empty))
                guid = Guid.NewGuid();

            Scene scene;
            Subscenes.Add(scene = new Scene() { ID = guid, Name = name, IsEnabled = enable, EntitytManager = new EntityManager() });

            return scene;
        }

        public static void RemoveSubscene(Guid guid)
        {
            foreach (var subscene in Subscenes)
                if (subscene.ID == guid)
                    Subscenes.Remove(subscene);
        }

        public static void LoadScene(Scene scene)
        {
            Scene = scene != null ? scene : new Scene() { ID = new Guid(), Name = "Core", IsEnabled = true, EntitytManager = new EntityManager() };
            Subscenes = new List<Scene>();
        }

        public void Awake()
        {
            Scene.Awake();

            foreach (var subscenes in Subscenes)
                subscenes.Awake();
        }

        public void Start()
        {
            Scene.Start();

            foreach (var subscenes in Subscenes)
                subscenes.Start();
        }

        public void Update()
        {
            Scene.Update();

            foreach (var subscenes in Subscenes)
                subscenes.Update();
        }
        
        public void LateUpdate()
        {
            Scene.LateUpdate();

            foreach (var subscenes in Subscenes)
                subscenes.LateUpdate();
        }

        public void Render()
        {
            Scene.Render();

            foreach (var subscenes in Subscenes)
                subscenes.Render();
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
