using System;
using Engine.Components;

namespace Engine.Utilities
{
    internal class Entity : ICloneable
    {
        internal Guid ID = Guid.NewGuid();

        internal Entity m_Parent;

        internal Transform m_Transform = new Transform();
        internal Material m_Material;
        internal Mesh m_Mesh;

        internal string m_Name = "Object";
        internal bool m_Enabled = true;
        internal bool m_Static = false;

        internal Entity Clone() { return (Entity)this.MemberwiseClone(); }
        object ICloneable.Clone() { return Clone(); }

        internal void Update_Render()
        {
            if (!m_Static)
            {
                if (m_Parent != null)
                    m_Transform.m_Parent = m_Parent.m_Transform;
                m_Transform.Update();
            }
            m_Material.Render(m_Transform.m_ConstantsBuffer);
            m_Mesh.Render();
        }
    }
}
