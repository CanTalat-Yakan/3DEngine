using System;
using System.Linq;
using System.Numerics;
using Editor.Controller;
using Engine.Components;
using Engine.Editor;

namespace Engine.Utilities
{
    internal class Scene : ICloneable
    {
        public Guid ID = Guid.NewGuid();

        public EntityManager EntitytManager = new EntityManager();

        public string Name = "Scene";
        public bool IsEnabled = true;

        private string _profile;

        object ICloneable.Clone() { return Clone(); }
        public Scene Clone()
        {
            var newScene = (Scene)this.MemberwiseClone();
            newScene.ID = Guid.NewGuid();

            return newScene;
        }

        public virtual void Awake() { }

        public virtual void Start() { }

        public virtual void Update() { }

        public virtual void LateUpdate() { }

        public void Load() { }

        public void Unload() { }

        public string Profile()
        {
            int vertexCount = 0;
            int indexCount = 0;

            foreach (var item in EntitytManager.EntityList)
                if (item.IsEnabled && item.Mesh != null)
                {
                    vertexCount += item.Mesh.VertexCount;
                    indexCount += item.Mesh.IndexCount;
                }

            _profile = "Objects: " + EntitytManager.EntityList.Count().ToString();
            _profile += "\n" + "Vertices: " + vertexCount;
            _profile += "\n" + "Indices: " + indexCount;

            return _profile;
        }

        public void Render()
        {
            foreach (var item in EntitytManager.EntityList)
                if (item.IsEnabled && item.Mesh != null)
                    item.Update_Render();

            if (EntitytManager.Sky != null)
                EntitytManager.Sky.Update_Render();
        }
    }
}
