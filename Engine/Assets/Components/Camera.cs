namespace Engine.Components;

public sealed class Camera : Component
{
    public static Camera Main { get; private set; }

    public CameraBuffers CameraBuffer { get; private set; } = new();

    public CameraProjection Projection = CameraProjection.Perspective;
    [Space]
    [If("Projection", "Perspective")]
    public float FOV = 90;
    [If("Projection", "Orthographic")]
    public float Size = 15;
    public Vector2 Clipping = new Vector2(0.1f, 1000f);
    [Space]
    public byte CameraID = 0;

    private Renderer _renderer => Renderer.Instance;

    public override void OnRegister() =>
        // Register the component with the CameraSystem.
        CameraSystem.Register(this);

    public Camera() =>
        //Create View Constant Buffer when Camera is initialized.
        CameraBuffer.View = _renderer.Device.CreateConstantBuffer<ViewConstantBuffer>();

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
        var projection = Projection == CameraProjection.Perspective
            ? Matrix4x4.CreatePerspectiveFieldOfView(radHFOV, aspect, Clipping.X, Clipping.Y)
            : Matrix4x4.CreateOrthographic(Size * aspect, Size, Clipping.X, Clipping.Y);

        // Calculate the view-projection matrix for the camera.
        var viewProjection = Matrix4x4.Transpose(view * projection);

        // Store the camera's view-projection matrix and position.
        CameraBuffer.ViewConstantBuffer = new()
        {
            ViewProjection = viewProjection,
            CameraPosition = Entity.Transform.Position,
        };

        /* The coordinate system used in System.Numerics is right-handed,
         * so adjustments need to be made for DirectX's left-handed coordinate system.
         * The index order must be flipped from 0,1,2 to 0,2,1.
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

        //Update constant buffer data
        CameraBuffer.UpdateConstantBuffer();
    }
}
