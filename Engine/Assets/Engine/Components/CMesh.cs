using System;
using System.Linq;
using WinUI3DEngine.Assets.Engine.Data;
using WinUI3DEngine.Assets.Engine.Utilities;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;

namespace WinUI3DEngine.Assets.Engine.Components
{
    internal class CMesh
    {
        CRenderer m_d3d;

        internal ID3D11Buffer m_VertexBuffer;
        internal ID3D11Buffer m_IndexBuffer;

        internal int m_VertexCount;
        internal int m_VertexStride;
        internal int m_IndexCount;
        internal int m_IndexStride;


        internal CMesh(CMeshInfo _obj)
        {
            #region //Get Instance of DirectX
            m_d3d = CRenderer.Instance;
            #endregion

            #region //Set Variables
            m_VertexCount = _obj.Vertices.Count();
            m_VertexStride = Unsafe.SizeOf<CVertex>();

            m_IndexCount = _obj.Indices.Count();
            m_IndexStride = Unsafe.SizeOf<int>();
            #endregion

            #region //Create VertexBuffer
            m_VertexBuffer = m_d3d.m_Device.CreateBuffer(
                _obj.Vertices.ToArray(),
                BindFlags.VertexBuffer);
            #endregion

            #region //Create IndexBuffer
            m_IndexBuffer = m_d3d.m_Device.CreateBuffer(
                _obj.Indices.ToArray(),
                BindFlags.IndexBuffer);
            #endregion
        }

        internal void Render()
        {
            m_d3d.RenderMesh(
                m_VertexBuffer, m_VertexStride,
                m_IndexBuffer, m_IndexCount);
        }
    }
}
