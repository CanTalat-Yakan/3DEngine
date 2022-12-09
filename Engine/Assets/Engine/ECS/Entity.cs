using System;
using System.Collections.Generic;
using Engine.Components;

namespace Engine.ECS
{
    internal class Entity : ICloneable
    {
        public Guid ID = Guid.NewGuid();

        public Entity Parent;

        public Transform Transform { get => GetComponent<Transform>(); }
        //public Transform Transform = new Transform();
        //public Material Material;
        //public Mesh Mesh;

        public string Name = "Object";
        public bool IsEnabled = true;
        public bool IsStatic = false;

        private List<Component> _components = new List<Component>();

        public Entity()
        {
            AddComponent(new Transform());
            //AddComponent(new Material());
            //AddComponent(new Mesh());
        }

        public void AddComponent(Component component)
        {
            _components.Add(component);
            component.Entity = this;
        }

        public void RemoveComponent(Component component)
        {
            _components.Remove(component);
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

        public Component[] GetComponents()
        {
            return _components.ToArray();
        }

        object ICloneable.Clone() { return Clone(); }

        public Entity Clone()
        {
            var newEntity = (Entity)MemberwiseClone();
            newEntity.ID = Guid.NewGuid();
            //newEntity.Transform = new Transform() { Position = Transform.Position, Rotation = Transform.Rotation, Scale = Transform.Scale };

            return newEntity;
        }
        //public void Update_Render()
        //{
        //    if (!IsStatic)
        //    {
        //        if (Parent != null)
        //            Transform.Parent = Parent.Transform;
        //        Transform.Update();
        //    }
        //    Material.Render(Transform.ConstantsBuffer);
        //    Mesh.Render();
        //}
    }
}
