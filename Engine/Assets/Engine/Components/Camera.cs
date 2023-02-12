using System.Numerics;
using System.Runtime.CompilerServices;
using System;
using Vortice.Direct3D11;
using Engine.Data;
using Engine.ECS;
using Engine.Utilities;
using Engine.Helper;

namespace Engine.Components
{
    internal class Camera : Component
    {
        public static Camera Main { get; private set; }

        public float FieldOfView = 90;
        public byte CameraOrder = 0;

        private Renderer _d3d => Renderer.Instance;

        private ID3D11Buffer _view;
        private SViewConstantBuffer _viewConstantBuffer;

        public override void OnRegister() =>
            // Register the component with the CameraSystem.
            CameraSystem.Register(this);

        public Camera() =>
            //Create View Constant Buffer when Camera is intialized.
            _view = _d3d.Device.CreateConstantBuffer<SViewConstantBuffer>();

        public override void OnAwake()
        {
            // Assign this camera instance as the main camera if it has "MainCamera" tag.
            if (Entity.Tag == ETags.MainCamera.ToString())
                Main = this;
        }

        public override void OnUpdate() =>
            // Override the Component Order with the local variable.
            Order = CameraOrder;

        public override void OnRender() =>
            // Recreates the view constants data to be used by the Camera.
            RecreateViewConstants();

        public void RecreateViewConstants()
        {            
            #region //Set ViewConstantBuffer
            // Calculate the view matrix to use for the camera.
            var view = Matrix4x4.CreateLookAt(
                Entity.Transform.Position,
                Entity.Transform.Position + Entity.Transform.Forward,
                Vector3.UnitY);

            // Get the aspect ratio for the device's screen.
            var aspect = (float)(_d3d.SwapChainPanel.ActualWidth / _d3d.SwapChainPanel.ActualHeight);
            var dAspect = aspect < 1 ? 1 * aspect : 1 / aspect;

            // Convert the field of view from degrees to radians.
            var radAngle = FieldOfView.ToRadians();
            var radHFOV = 2 * MathF.Atan(MathF.Tan(radAngle * 0.5f) * dAspect);
            var hFOV = radHFOV.ToDegrees();

            // Calculate the projection matrix for the camera.
            var projection = Matrix4x4.CreatePerspectiveFieldOfView(radHFOV, aspect, 0.1f, 1000);

            // Calculate the view-projection matrix for the camera.
            var viewProjection = Matrix4x4.Transpose(view * projection);

            // Store the camera's view-projection matrix and position.
            _viewConstantBuffer = new()
            {
                ViewProjection = viewProjection,
                CameraPositon = Entity.Transform.Position,
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
                // Map the constant buffer to memory for write access.
                MappedSubresource mappedResource = _d3d.DeviceContext.Map(this._view, MapMode.WriteDiscard);
                // Copy the data from the constant buffer to the mapped resource.
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref _viewConstantBuffer);
                // Unmap the constant buffer from memory.
                _d3d.DeviceContext.Unmap(this._view, 0);
            }

            // Set the constant buffer to be used by the vertex shader.
            _d3d.DeviceContext.VSSetConstantBuffer(0, this._view);
            #endregion
        }
    }
}
