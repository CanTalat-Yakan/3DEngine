using Vortice.Mathematics;

namespace Engine.Components;

public sealed partial class Transform : EditorComponent, IHide
{
    public bool TransformChanged { get; set; }

    public Transform Parent => Entity.Parent?.Transform;

    public Vector3 Forward => TransformVector3(LocalForward, Parent?.Forward);
    public Vector3 LocalForward = Vector3.UnitZ;

    public Vector3 Right => TransformVector3(LocalRight, Parent?.Right);
    public Vector3 LocalRight = Vector3.UnitX;

    public Vector3 Up => TransformVector3(LocalUp, Parent?.Right);
    public Vector3 LocalUp = Vector3.UnitY;

    public Vector3 Position => TransformVector3(LocalPosition, Parent?.Position);
    public Vector3 LocalPosition = Vector3.Zero;

    public Quaternion Rotation => MultiplyQuaternion(LocalRotation, Parent?.Rotation);
    public Quaternion LocalRotation { get => _localRotation; set => SetQuaternion(value); }
    private Quaternion _localRotation = Quaternion.Identity;

    public Vector3 EulerAngles { get => _eulerAngles; set => SetEulerAngles(value); }
    private Vector3 _eulerAngles = Vector3.Zero;

    public Vector3 Scale => MultiplyVector3(LocalScale, Parent?.Scale);
    public Vector3 LocalScale = Vector3.One;

    public Matrix4x4 WorldMatrix => _worldMatrix;
    private Matrix4x4 _worldMatrix = Matrix4x4.Identity;
    private Matrix4x4 _previousWorldMatrix = Matrix4x4.Identity;

    private PerModelConstantBuffer _modelConstantBuffer;

    public override void OnRegister() =>
        TransformSystem.Register(this);

    public override void OnAwake() =>
        TransformChanged = true;

    public override void OnUpdate()
    {
        // Calculate the forward, right and up direction.
        CalculateOrientation();

        _previousWorldMatrix = WorldMatrix;

        // Calculate the world matrix as a result of the local position, rotation and scale.
        CalculateWorldMatrix();

        if (!WorldMatrix.Equals(_previousWorldMatrix))
            TransformChanged = true;
    }

    public override void OnRender() =>
        TransformChanged = false;

    public override string ToString() =>
        $"""
        {LocalPosition}
        {EulerAngles}
        {LocalScale}
        """;
}

public sealed partial class Transform : EditorComponent, IHide
{
    internal PerModelConstantBuffer GetConstantBuffer()
    {
        // Set the transposed ModelView matrix in the ModelConstantBuffer to the WorldMatrix.
        _modelConstantBuffer = new(
            Matrix4x4.Transpose(_worldMatrix));

        return _modelConstantBuffer;
    }

    private void CalculateWorldMatrix()
    {
        // Get the translation matrix based on the local position.
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(LocalPosition);
        // Get the rotation matrix based on the local rotation.
        Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(LocalRotation);
        // Get the scale matrix based on the local scale.
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(LocalScale);

        // Get the world matrix from the multiplication of scale, rotation and translation.
        _worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;

        // Multiply by the world matrix of the parent if it exists.
        if (Parent is not null)
            _worldMatrix *= Parent.WorldMatrix;
    }

    private void CalculateOrientation()
    {
        // Calculate the forward direction based on the EulerAngles.
        LocalForward = CalculateForward(LocalForward, EulerAngles);

        // Calculate the right direction as a product of the forward and global up direction.
        LocalRight = Vector3.Normalize(Vector3.Cross(LocalForward, Vector3.UnitY));

        // Calculate the local up direction as a product of the right and forward direction.
        LocalUp = Vector3.Normalize(Vector3.Cross(LocalRight, LocalForward));
    }

    private Vector3 CalculateForward(Vector3 input, Vector3 eulerAngles)
    {
        float sinX = MathF.Sin(eulerAngles.X.ToRadians());
        float cosX = MathF.Cos(eulerAngles.X.ToRadians());
        float sinY = MathF.Sin(eulerAngles.Y.ToRadians());
        float cosY = MathF.Cos(eulerAngles.Y.ToRadians());

        return Vector3.Normalize(input.SetVector(
            sinY * cosX,
            -sinX,
            cosY * cosX));
    }
}

public sealed partial class Transform : EditorComponent, IHide
{
    private Vector3 TransformVector3(Vector3 local, Vector3? parent)
    {
        if (Parent is null)
            return local;

        return Vector3.Transform(local, Parent.Rotation) + parent.Value;
    }

    private Vector3 MultiplyVector3(Vector3 local, Vector3? parent)
    {
        if (Parent is null)
            return local;

        return Vector3.Multiply(local, parent.Value);
    }

    private Quaternion MultiplyQuaternion(Quaternion local, Quaternion? parent)
    {
        if (Parent is null)
            return local;

        return Quaternion.Multiply(local, parent.Value);
    }

    private void SetEulerAngles(Vector3 value)
    {
        // Set the values for the EulerAngles in degrees.
        _eulerAngles = value;

        // Set the LocalRotation with the new quaternion from the Euler in radians.
        _localRotation = value.ToRadians().FromEuler();
    }

    private void SetQuaternion(Quaternion value)
    {
        // Set the LocalRotation with the quaternion.
        _localRotation = value;

        // Set the EulerAngles with the new Euler from the quaternion.
        _eulerAngles = value.ToEuler();
    }
}