using System.Runtime.CompilerServices;

using Vortice.Direct3D11;

namespace Engine.Components;

public sealed class Camera : Component
{
    public static Camera Main { get; private set; }

    public float FOV = 90;
    public byte CameraID = 0;

    private ID3D11Buffer _view;
    private ViewConstantBuffer _viewConstantBuffer;

    private Renderer _renderer => Renderer.Instance;

    public override void OnRegister() =>
        // Register the component with the CameraSystem.
        CameraSystem.Register(this);

    public Camera() =>
        //Create View Constant Buffer when Camera is initialized.
        _view = _renderer.Device.CreateConstantBuffer<ViewConstantBuffer>();

    public override void OnAwake()
    {
        // Assign this camera instance as the main camera if it has "MainCamera" tag.
        if (Entity.Tag == Tags.MainCamera.ToString())
            Main = this;
    }

    public override void OnUpdate() =>
        // Override the Component Order with the local variable.
        Order = CameraID;

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
        var aspect = (float)_renderer.Size.Width / (float)_renderer.Size.Height;
        var dAspect = aspect < 1 ? 1 * aspect : 1 / aspect;

        // Convert the field of view from degrees to radians.
        var radAngle = (FOV).ToRadians();
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
            CameraPosition = Entity.Transform.Position,
        };
        #endregion

        /* The coordinate system used in System.Numerics is right-handed,
         * so adjustments need to be made for DirectX's left-handed coordinate system.
         * The index order must be flipped from 0,1,2 to 0,2,1,
         * (Renderer:SetRasterizerDesc FrontCounterClockWise = true)
         * and the texture coordinates in DirectX are flipped along the Y-axis,
         * with 0,0 starting in the top-left corner and going down to 0,1.
         * (ModelLoader:LoadFile postProcessSteps = FlipUV)
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
            MappedSubresource mappedResource = _renderer.Data.DeviceContext.Map(_view, MapMode.WriteDiscard);
            // Copy the data from the constant buffer to the mapped resource.
            Unsafe.Copy(mappedResource.DataPointer.ToPointer(), ref _viewConstantBuffer);
            // Unmap the constant buffer from memory.
            _renderer.Data.DeviceContext.Unmap(_view, 0);
        }
        #endregion

        // Set the constant buffer in the vertex shader stage of the device context.
        _renderer.Data.SetConstantBuffer(0, _view);
    }
}
