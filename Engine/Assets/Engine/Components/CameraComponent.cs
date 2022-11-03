﻿using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using Engine.Data;
using Engine.Utilities;

namespace Engine.Components
{
    internal class CameraComponent
    {
        public static double s_FieldOfView;
        public SViewConstantsBuffer ViewConstants;

        public TransformComponent Transform = new TransformComponent();

        private Renderer _d3d;
        private ID3D11Buffer _view;

        public CameraComponent()
        {
            #region //Get Instances
            _d3d = Renderer.Instance;
            #endregion

            _view = _d3d.Device.CreateConstantBuffer<SViewConstantsBuffer>();

            RecreateViewConstants();
        }

        public void RecreateViewConstants()
        {
            Transform.Update();

            #region //Set ViewConstantBuffer
            var view = Matrix4x4.CreateLookAt(
                Transform.Position,
                Transform.Position + Transform.Forward,
                Vector3.UnitY);

            var aspect = (float)(_d3d.SwapChainPanel.ActualWidth / _d3d.SwapChainPanel.ActualHeight);
            var dAspect = aspect < 1 ? 1 * aspect : 1 / aspect;

            var radAngle = MathHelper.ToRadians((float)s_FieldOfView);
            var radHFOV = 2 * MathF.Atan(MathF.Tan(radAngle * 0.5f) * dAspect);
            var hFOV = MathHelper.ToDegrees(radHFOV);

            var projection = Matrix4x4.CreatePerspectiveFieldOfView(radHFOV, aspect, 0.1f, 1000);

            // For using the right-handed coord-system used with System.Numerics, we have to adjust the DirectX left-handed coord-system
            // The order of indice has to flip counter-clockwise from 0,1,2 to 0,2,1
            // The DirectX texture Coords is flipped in the Y-Axis, as it start in the top-left corner with 0,0 and goes down to 0,1
            // The camera now uses the X-Mouseaxis without negating it

            // T = Transpose
            // (World * View * Projection)T = ProjectionT * ViewT * WorldT. World is in PerModelConstantBuffer
            // T(v*p), because HLSL calculates matrix mul in collumn-major and system.numerics returns row-major
            var viewProjection = Matrix4x4.Transpose(view * projection); 

            ViewConstants = new SViewConstantsBuffer() { ViewProjection = viewProjection, CameraPisiton = Transform.Position };
            #endregion

            unsafe
            {
                // Update constant buffer data
                MappedSubresource mappedResource = _d3d.DeviceContext.Map(this._view, MapMode.WriteDiscard);
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref ViewConstants);
                _d3d.DeviceContext.Unmap(this._view, 0);
            }
            _d3d.DeviceContext.VSSetConstantBuffer(0, this._view);
        }
    }
}