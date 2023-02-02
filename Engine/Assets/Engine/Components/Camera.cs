using System.Numerics;
using System.Runtime.CompilerServices;
using System;
using Vortice.Direct3D11;
using Vortice.Mathematics;
using Engine.Data;
using Engine.ECS;
using Engine.Utilities;

namespace Engine.Components
{
    internal class Camera : Component
    {
        public static Camera Main { get; private set; }

        public double FieldOfView = 90;

        private Renderer _d3d { get => Renderer.Instance; }

        private ID3D11Buffer _view;
        private SViewConstantsBuffer _viewConstants;

        public override void OnRegister() =>
            CameraSystem.Register(this);

        public Camera() =>
            _view = _d3d.Device.CreateConstantBuffer<SViewConstantsBuffer>();

        public override void OnAwake()
        {
            if (_entity.Tag == ETags.MainCamera.ToString())
                Main = this;
        }

        public override void OnLateUpdate() => 
            RecreateViewConstants();

        public void RecreateViewConstants()
        {
            #region //Set ViewConstantBuffer
            var view = Matrix4x4.CreateLookAt(
                _entity.Transform.Position,
                _entity.Transform.Position + _entity.Transform.Forward,
                Vector3.UnitY);

            var aspect = (float)(_d3d.SwapChainPanel.ActualWidth / _d3d.SwapChainPanel.ActualHeight);
            var dAspect = aspect < 1 ? 1 * aspect : 1 / aspect;

            var radAngle = MathHelper.ToRadians((float)FieldOfView);
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

            _viewConstants = new() { ViewProjection = viewProjection, CameraPositon = _entity.Transform.Position };
            #endregion

            #region //Update constant buffer data
            unsafe
            {
                MappedSubresource mappedResource = _d3d.DeviceContext.Map(this._view, MapMode.WriteDiscard);
                Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref _viewConstants);
                _d3d.DeviceContext.Unmap(this._view, 0);
            }
            _d3d.DeviceContext.VSSetConstantBuffer(0, this._view);
            #endregion
        }
    }
}
