using System.Numerics;
using System.Runtime.CompilerServices;
using System;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using Engine.Data;
using Engine.ECS;
using Engine.Utilities;
using Editor.Controller;
using BenchmarkDotNet.Columns;
using Microsoft.UI.Xaml.Media;
using System.Net.NetworkInformation;
using Vortice.WIC;

namespace Engine.Components
{
    internal class Camera : Component
    {
        public static Camera Main { get; private set; }

        public float FieldOfView = 90;

        private Renderer _d3d { get => Renderer.Instance; }

        private ID3D11Buffer _view;
        private SViewConstantsBuffer _viewConstants;

        public override void OnRegister() =>
            // Register the script with the CameraSystem.
            CameraSystem.Register(this);

        public Camera() =>
            //Create View Constant Buffer when Camera is intialized.
            _view = _d3d.Device.CreateConstantBuffer<SViewConstantsBuffer>();

        public override void OnAwake()
        {
            // Assign this camera instance as the main camera if it has "MainCamera" tag.
            if (Entity.Tag == ETags.MainCamera.ToString())
                Main = this;
        }

        public override void OnRender() =>
            // Recreates the view constants data to be used by the Camera.
            RecreateViewConstants();

        public void RecreateViewConstants()
        {
            #region //Set ViewConstantBuffer
            // Calculate the view matrix to use for the camera.
            var view = Matrix4x4.CreateLookAt(
                Entity.Transform.WorldPosition,
                Entity.Transform.WorldPosition + Entity.Transform.Forward,
                Vector3.UnitY);

            // Get the aspect ratio for the device's screen.
            var aspect = (float)(_d3d.SwapChainPanel.ActualWidth / _d3d.SwapChainPanel.ActualHeight);
            var dAspect = aspect < 1 ? 1 * aspect : 1 / aspect;

            // Convert the field of view from degrees to radians.
            var radAngle = MathHelper.ToRadians((float)FieldOfView);
            var radHFOV = 2 * MathF.Atan(MathF.Tan(radAngle * 0.5f) * dAspect);
            var hFOV = MathHelper.ToDegrees(radHFOV);

            // Calculate the projection matrix for the camera.
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(radHFOV, aspect, 0.1f, 1000);

            // Calculate the view-projection matrix for the camera.
            var viewProjection = Matrix4x4.Transpose(view * projection);

            // Store the camera's view-projection matrix and position.
            _viewConstants = new()
            {
                ViewProjection = viewProjection,
                CameraPositon = Entity.Transform.Position
            };
            #endregion

            /* The coordinate system used in System.Numerics is right-handed,
             * so adjustments need to be made for DirectX's left-handed coordinate system.
             * The index order must be flipped from 0,1,2 to 0,2,1,
             * and the texture coordinates in DirectX are flipped along the Y-axis,
             * with 0,0 starting in the top-left corner and going down to 0,1.
             * The X-mouse axis is also used without negation.
               
             * To match the HLSL calculations, which use column - major matrices,
             * the transpose of(World * View * Projection) is calculated.
             * The calculation becomes ProjectionT *ViewT * WorldT,
             * with World being in the PerModelConstantBuffer.
             * The transpose of (View * Projection) is also taken
             * because HLSL calculates matrix multiplication in column - major form
             * and System.Numerics returns row - major.
             */

            #region //Update constant buffer data
            // Map the constant buffer and copy the camera's view-projection matrix and position into it.
            unsafe
            {
                MappedSubresource mappedResource = _d3d.DeviceContext.Map(this._view, MapMode.WriteDiscard);
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref _viewConstants);
                _d3d.DeviceContext.Unmap(this._view, 0);
            }

            // Set the constant buffer to be used by the vertex shader.
            _d3d.DeviceContext.VSSetConstantBuffer(0, this._view);
            #endregion
        }
    }
}
