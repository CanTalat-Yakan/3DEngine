using System.Linq;
using System;
using Engine.Components;

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

        public void Load() { }

        public void Unload() { }

        public string Profile()
        {
            int vertexCount = 0;
            int indexCount = 0;

            foreach (var entoty in EntitytManager.EntityList)
            {
                var mesh = entoty.GetComponent<Mesh>();
                if (entoty.IsEnabled && mesh != null)
                {
                    vertexCount += mesh.VertexCount;
                    indexCount += mesh.IndexCount;
                }
            }

            _profile = "Objects: " + EntitytManager.EntityList.Count().ToString();
            _profile += "\n" + "Vertices: " + vertexCount;
            _profile += "\n" + "Indices: " + indexCount;

            return _profile;
        }

    }
}
