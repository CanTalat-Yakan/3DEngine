using System;
using WinUI3DEngine.Assets.Engine.Components;

namespace WinUI3DEngine.Assets.Engine.Utilities
{
    internal class CObject : ICloneable
    {
        internal Guid ID = Guid.NewGuid();

        internal CObject m_Parent;

        internal CTransform m_Transform = new CTransform();
        internal CMaterial m_Material;
        internal CMesh m_Mesh;

        internal string m_Name = "Object";
        internal bool m_Enabled = true;
        internal bool m_Static = false;

        internal CObject Clone() { return (CObject)this.MemberwiseClone(); }
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
