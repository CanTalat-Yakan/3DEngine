using System;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using System.Xml.Schema;
using Microsoft.UI.Xaml.Media;
using Engine.Data;
using Engine.Utilities;

namespace Engine.Components
{
    internal class Camera
    {
        Renderer d3d;

        internal static double s_fieldOfView;
        internal SViewConstantsBuffer viewConstants;
        ID3D11Buffer view;

        internal Transform transform = new Transform();

        internal Camera()
        {
            #region //Get Instances
            d3d = Renderer.Instance;
            #endregion

            view = d3d.device.CreateConstantBuffer<SViewConstantsBuffer>();

            RecreateViewConstants();
        }

        internal void RecreateViewConstants()
        {
            transform.Update();

            #region //Set ViewConstantBuffer
            var view = Matrix4x4.CreateLookAt(
                transform.position,
                transform.position + transform.forward,
                Vector3.UnitY);

            var aspect = (float)(d3d.swapChainPanel.ActualWidth / d3d.swapChainPanel.ActualHeight);
            var dAspect = aspect < 1 ? 1 * aspect : 1 / aspect;

            var radAngle = MathHelper.ToRadians((float)s_fieldOfView);
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

            viewConstants = new SViewConstantsBuffer() { ViewProjection = viewProjection, CameraPisiton = transform.position };
            #endregion

            unsafe
            {
                // Update constant buffer data
                MappedSubresource mappedResource = d3d.deviceContext.Map(this.view, MapMode.WriteDiscard);
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref viewConstants);
                d3d.deviceContext.Unmap(this.view, 0);
            }
            d3d.deviceContext.VSSetConstantBuffer(0, this.view);
        }
    }
}
