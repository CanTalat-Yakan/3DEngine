using Vortice.Mathematics;

namespace Engine.Components;

public sealed class Transform : Component, IHide
{
    public Transform Parent => Entity.Parent is null ? null : Entity.Parent.Transform;

    public Vector3 Forward => Parent is null ? LocalForward : Vector3.Transform(LocalForward, Parent.Rotation) + Parent.Forward;
    public Vector3 LocalForward = Vector3.UnitZ;

    public Vector3 Right => Parent is null ? LocalRight : Vector3.Transform(LocalRight, Parent.Rotation) + Parent.Right;
    public Vector3 LocalRight = Vector3.UnitX;

    public Vector3 Up => Parent is null ? LocalUp : Vector3.Transform(LocalUp, Parent.Rotation) + Parent.Right;
    public Vector3 LocalUp = Vector3.UnitY;

    public Vector3 Position => Parent is null ? LocalPosition : Vector3.Transform(LocalPosition, Parent.Rotation) + Parent.Position;
    public Vector3 LocalPosition = Vector3.Zero;

    public Quaternion Rotation => Parent is null ? LocalRotation : LocalRotation * Parent.Rotation;
    public Quaternion LocalRotation { get => _localRotation; set => SetQuaternion(value); }
    private Quaternion _localRotation = Quaternion.Identity;

    public Vector3 EulerAngles { get => _eulerAngles; set => SetEulerAngles(value); }
    private Vector3 _eulerAngles = Vector3.Zero;

    public Vector3 Scale => Parent is null ? LocalScale : LocalScale * Parent.Scale;
    public Vector3 LocalScale = Vector3.One;

    public Matrix4x4 WorldMatrix => _worldMatrix;
    private Matrix4x4 _worldMatrix;

    private PerModelConstantBuffer _modelConstantBuffer;

    public override void OnRegister() =>
        // Register the component with the TransformSystem.
        TransformSystem.Register(this);

    public override void OnUpdate()
    {
        // Calculate the forward direction based on the EulerAngles.
        LocalForward = Vector3.Normalize(new(
            MathF.Sin(EulerAngles.Y.ToRadians()) * MathF.Cos(EulerAngles.X.ToRadians()),
            MathF.Sin(-EulerAngles.X.ToRadians()),
            MathF.Cos(EulerAngles.X.ToRadians()) * MathF.Cos(EulerAngles.Y.ToRadians())));
        // Calculate the right direction based on the forward direction.
        LocalRight = Vector3.Normalize(Vector3.Cross(LocalForward, Vector3.UnitY));
        // Calculate the local up direction based on the forward and right direction.
        LocalUp = Vector3.Normalize(Vector3.Cross(LocalRight, LocalForward));

        // Calculate the translation matrix based on the WorldPosition.
        Matrix4x4 translationMatrix = Matrix4x4.CreateTranslation(Position);
        // Calculate the rotation matrix based on the WorldRotation.
        Matrix4x4 rotationMatrix = Matrix4x4.CreateFromQuaternion(Rotation);
        // Calculate the scale matrix based on the WorldScale.
        Matrix4x4 scaleMatrix = Matrix4x4.CreateScale(Scale);

        // Calculate the world matrix as a result of the combination of scale, rotation and translation matrices.
        _worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;
    }

    internal PerModelConstantBuffer GetConstantBuffer()
    {
        // Set the transposed ModelView matrix in the ModelConstantBuffer to the WorldMatrix.
        _modelConstantBuffer.ModelView = Matrix4x4.Transpose(_worldMatrix);

        return _modelConstantBuffer;
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

    public string GetString() =>
        $"""
            {LocalPosition}
            {EulerAngles}
            {LocalScale}
            """;
}
