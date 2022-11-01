using System;
using Engine.Components;

namespace Engine.Utilities
{
    internal class Entity : ICloneable
    {
        internal Guid id = Guid.NewGuid();

        internal Entity parent;

        internal Transform transform = new Transform();
        internal Material material;
        internal Mesh mesh;

        internal string name = "Object";
        internal bool isEnabled = true;
        internal bool isStatic = false;

        internal Entity Clone() { return (Entity)this.MemberwiseClone(); }
        object ICloneable.Clone() { return Clone(); }

        internal void Update_Render()
        {
            if (!isStatic)
            {
                if (parent != null)
                    transform.parent = parent.transform;
                transform.Update();
            }
            material.Render(transform.m_ConstantsBuffer);
            mesh.Render();
        }
    }
}
