using System.Collections.Generic;
using System;
using Engine.Components;
using System.Reflection;
using Engine.Utilities;

namespace Engine.ECS
{
    internal enum EEditorTags
    {
        SceneBoot,
        SceneCamera,
        SceneSky,
    }

    internal enum ETags
    {
        Untagged,
        MainCamera,
        Respawn,
        Player,
        Finish,
        GameController,
    }

    internal enum ELayers
    {
        Default,
        TransparentFX,
        IgnoreRaycast,
        Water,
        UI
    }

    internal class Entity : ICloneable
    {
        public Guid ID = Guid.NewGuid();

        public Entity Parent;

        public string Name;
        public bool IsEnabled = true;
        public bool IsStatic = false;
        public string Tag;
        public string Layer;

        private Scene _scene;
        public Scene Scene { get => _scene is null ? _scene = SceneManager.GetFromEntityID(ID) : _scene; set => _scene = null; }

        private Transform _transform;
        public Transform Transform { get => _transform; }

        private bool _activeInHierarchy = true;
        public bool ActiveInHierarchy { get => Parent != null ? _activeInHierarchy &= Parent.IsEnabled : IsEnabled; }

        private List<Component> _components = new();

        public Entity() =>
            AddComponent(_transform = new Transform());

        public void AddComponent(Component component)
        {
            _components.Add(component);
            component.Entity = this;
            component.IsEnabled = true;
        }

        public void RemoveComponent(Component component)
        {
            _components.Remove(component);
            component.InvokeEventOnDestroy();
        }

        public void RemoveComponents()
        {
            foreach (var component in _components)
                component.InvokeEventOnDestroy();

            _components.Clear();
        }

        public T GetComponent<T>() where T : Component
        {
            foreach (var component in _components)
                if (component.GetType().Equals(typeof(T)))
                    return (T)component;

            return null;
        }

        public T[] GetComponents<T>() where T : Component
        {
            List<T> components = new();
            foreach (var component in _components)
                if (component.GetType().Equals(typeof(T)))
                    components.Add((T)component);

            return components.ToArray();
        }

        public bool CompareTag(params string[] tags)
        {
            foreach (var tag in tags)
                if (Tag == tag)
                    return true;

            return false;
        }

        public Component[] GetComponents() { return _components.ToArray(); }

        object ICloneable.Clone() { return Clone(); }

        public Entity Clone()
        {
            //var newEntity = (Entity)MemberwiseClone();
            //newEntity.ID = Guid.NewGuid();

            var newEntity = new Entity()
            {
                ID = Guid.NewGuid(),
                Name = Name,
                IsEnabled = IsEnabled,
                IsStatic = IsStatic,
                Layer = Layer,
                Tag = Tag,
            };

            newEntity.Transform.Position = Transform.Position;
            newEntity.Transform.Rotation = Transform.Rotation;
            newEntity.Transform.Scale = Transform.Scale;

            for (int i = 1; i < _components.Count; i++)
            {
                var newComponent = _components[i].Clone();
                newComponent.OnRegister();
                newEntity.AddComponent(newComponent);
            }

            return newEntity;
        }
    }
}
