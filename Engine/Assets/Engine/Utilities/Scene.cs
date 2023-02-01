using System.Linq;
using System;
using Editor.Controller;
using Engine.Components;

namespace Engine.Utilities
{
    internal class Scene : BindableBase, ICloneable
    {
        public Guid ID = Guid.NewGuid();

        public EntityManager EntitytManager = new();


        private string _name = "Scene";
        public string Name
        {
            get { return _name; }
            set { SetProperty(ref _name , value); }
        }

        private bool _isEnabled = true;
        public bool IsEnabled
        {
            get { return _isEnabled; }
            set { SetProperty(ref _isEnabled, value); }
        }

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

            foreach (var entity in EntitytManager.EntityList)
            {
                var mesh = entity.GetComponent<Mesh>();
                if (entity.IsEnabled && mesh != null)
                {
                    vertexCount += mesh._vertexCount;
                    indexCount += mesh._indexCount;
                }
            }

            _profile = "Objects: " + EntitytManager.EntityList.Count().ToString();
            _profile += "\n" + "Vertices: " + vertexCount;
            _profile += "\n" + "Indices: " + indexCount;

            return _profile;
        }

    }
}
