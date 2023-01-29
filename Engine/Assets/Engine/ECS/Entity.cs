using System.Collections.Generic;
using System;
using Engine.Components;
using System.Reflection;

namespace Engine.ECS
{
    internal enum ETags
    {
        Untagged,
        MainCamera,
        Respawn,
        Player,
        Finish,
        GameController
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

        public Transform Transform { get => _transform; }

        public string Name;
        public bool IsEnabled;
        public bool IsStatic;
        public ETags Tag;
        public ELayers Layer;

        private List<Component> _components = new();
        private Transform _transform;

        public Entity() => AddComponent(_transform = new Transform());

        public void AddComponent(Component component)
        {
            _components.Add(component);
            component._entity = this;
        }

        public void RemoveComponent(Component component)
        {
            _components.Remove(component);

            component.InvokeOnDestroy();
        }

        public T GetComponent<T>() where T : Component
        {
            foreach (Component component in _components)
                if (component.GetType().Equals(typeof(T)))
                    return (T)component;

            return null;
        }

        public T[] GetComponents<T>() where T : Component
        {
            List<T> components = new();
            foreach (Component component in _components)
                if (component.GetType().Equals(typeof(T)))
                    components.Add((T)component);

            return components.ToArray();
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
                newComponent.GetType().GetConstructor(Type.EmptyTypes).Invoke(newComponent, null);
                newEntity.AddComponent(newComponent);
            }

            return newEntity;
        }
    }
}
