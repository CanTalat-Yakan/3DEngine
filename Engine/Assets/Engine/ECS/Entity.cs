using System.Collections.Generic;
using System;
using Engine.Components;

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

        public Transform Transform { get => GetComponent<Transform>(); }

        public string Name = "Entity";
        public bool IsEnabled = true;
        public bool IsStatic = false;
        public ETags Tag;
        public ELayers Layer;

        private List<Component> _components = new List<Component>();

        public Entity() => AddComponent(new Transform());

        public void AddComponent(Component component)
        {
            _components.Add(component);
            component.Entity = this;
        }

        public void RemoveComponent(Component component) => _components.Remove(component);

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

        public Component[] GetComponents()
        {
            return _components.ToArray();
        }

        object ICloneable.Clone()
        {
            return Clone();
        }

        public Entity Clone()
        {
            var newEntity = (Entity)MemberwiseClone();
            newEntity.ID = Guid.NewGuid();
            // clone all components

            return newEntity;
        }
    }
}
