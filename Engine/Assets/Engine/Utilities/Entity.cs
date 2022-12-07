using System;
using Engine.Components;

namespace Engine.Utilities
{
    internal class Entity : ICloneable
    {
        public Guid ID = Guid.NewGuid();

        public Entity Parent;

        public TransformComponent Transform = new TransformComponent();
        public MaterialComponent Material;
        public MeshComponent Mesh;

        public string Name = "Object";
        public bool IsEnabled = true;
        public bool IsStatic = false;

        object ICloneable.Clone() { return Clone(); }
        public Entity Clone()
        {
            var newEntity = (Entity)this.MemberwiseClone();
            newEntity.ID = Guid.NewGuid();
            newEntity.Transform = new TransformComponent() { Position = Transform.Position, Rotation = Transform.Rotation, Scale = Transform.Scale };

            return newEntity;
        }

        public void Update_Render()
        {
            if (!IsStatic)
            {
                if (Parent != null)
                    Transform.Parent = Parent.Transform;
                Transform.Update();
            }
            Material.Render(Transform.ConstantsBuffer);
            Mesh.Render();
        }
    }
}
