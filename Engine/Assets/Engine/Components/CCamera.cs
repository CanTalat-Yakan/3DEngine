using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using WinUI3DEngine.Assets.Engine.Data;
using WinUI3DEngine.Assets.Engine.Utilities;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using System.Xml.Schema;
using Microsoft.UI.Xaml.Media;

namespace WinUI3DEngine.Assets.Engine.Components
{
    internal class CCamera
    {
        CRenderer m_d3d;

        internal static double FieldOfView;
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

            #region //Set ViewConstantBuffer
            var view = Matrix4x4.CreateLookAt(
                m_Transform.m_Position,
                m_Transform.m_Position + m_Transform.Forward,
                Vector3.UnitY);

            var aspect = (float)(m_d3d.m_SwapChainPanel.ActualWidth / m_d3d.m_SwapChainPanel.ActualHeight);
            var dAspect = aspect < 1 ? 1 * aspect : 1 / aspect;

            var radAngle = MathHelper.ToRadians((float)FieldOfView);
            var radHFOV = 2 * MathF.Atan(MathF.Tan(radAngle * 0.5f) * dAspect);
            var hFOV = MathHelper.ToDegrees(radHFOV);

            var projection = Matrix4x4.CreatePerspectiveFieldOfView(radHFOV, aspect, 0.1f, 1000);

            // For using the right-handed coord-system used with System.Numerics, we have to adjust the DirectX left-handed coord-system
            // The order of indice has to flip counter-clockwise from 0,1,2 to 0,2,1
            // The DirectX texture Coords is flipped in the Y-Axis, as it start in the top-left corner with 0,0 and goes down to 0,1
            // The camera now use the X-Mouseaxis without negating it

            // T = Transpose
            // (World * View * Projection)T = ProjectionT * ViewT * WorldT. World is in PerModelConstantBuffer
            var viewProjection = Matrix4x4.Transpose(view * projection); // HLSL calculates matrix mul in collumn-major and system.numerics returns row-major

            m_ViewConstants = new SViewConstantsBuffer() { ViewProjection = viewProjection, CameraPisiton = m_Transform.m_Position };
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
