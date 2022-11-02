using System;
using Engine.Components;

namespace Engine.Utilities
{
    public class Entity : ICloneable
    {
        public Guid ID = Guid.NewGuid();

        public Entity Parent;

        public TransformComponent Transform = new TransformComponent();
        public MaterialComponent Material;
        public MeshComponent Mesh;

        public string Name = "Object";
        public bool IsEnabled = true;
        public bool IsStatic = false;

        public Entity Clone() { return (Entity)this.MemberwiseClone(); }
        object ICloneable.Clone() { return Clone(); }

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
