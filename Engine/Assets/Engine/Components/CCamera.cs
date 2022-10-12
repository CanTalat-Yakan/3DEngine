using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using WinUI3DEngine.Assets.Engine.Data;
using WinUI3DEngine.Assets.Engine.Utilities;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using System.Xml.Schema;

namespace WinUI3DEngine.Assets.Engine.Components
{
    internal class CCamera
    {
        CRenderer m_d3d;

        internal static double m_FOV;
        internal SViewConstantsBuffer m_ViewConstants;
        ID3D11Buffer m_view;

        internal CTransform m_Transform = new CTransform();

        internal CCamera()
        {
            #region //Get Instances
            m_d3d = CRenderer.Instance;
            #endregion

            m_view = m_d3d.m_Device.CreateConstantBuffer<SViewConstantsBuffer>();

            RecreateViewConstants();
        }

        internal void RecreateViewConstants()
        {
            m_Transform.Update();
            m_Transform.Right *= -1;

            #region //Set ViewConstantBuffer
            var view = Matrix4x4.CreateLookAt(
                m_Transform.m_Position + m_Transform.Forward,
                m_Transform.m_Position,
                Vector3.UnitY);

            var aspect = (float)(m_d3d.m_SwapChainPanel.ActualWidth / m_d3d.m_SwapChainPanel.ActualHeight);
            var fov = MathHelper.ToDegrees(2 * MathHelper.Atan(MathHelper.ToRadians(MathHelper.Tan(MathHelper.ToRadians((float)(m_FOV * 0.5)) * (aspect < 1 ? 1 * aspect : 1 / aspect)))));
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(fov, aspect, 0.1f, 1000);

            projection.M34 *= -1;
            projection.M43 *= -1;

            var viewProjection = Matrix4x4.Transpose(view * projection);

            m_ViewConstants = new SViewConstantsBuffer() { ViewProjection = viewProjection, World = m_Transform.m_Position };
            #endregion

            unsafe
            {
                // Update constant buffer data
                MappedSubresource mappedResource = m_d3d.m_DeviceContext.Map(m_view, MapMode.WriteDiscard);
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref m_ViewConstants);
                m_d3d.m_DeviceContext.Unmap(m_view, 0);
            }
            m_d3d.m_DeviceContext.VSSetConstantBuffer(0, m_view);
        }
    }
}
