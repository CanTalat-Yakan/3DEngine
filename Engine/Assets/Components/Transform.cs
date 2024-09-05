using Vortice.Mathematics;

namespace Engine.Components;

public struct PerModelConstantBuffer(Matrix4x4 modelView)
{
    public Matrix4x4 ModelView = modelView;
}

public sealed partial class Transform : EditorComponent, IHide
{
    public Action TransformChanged { get; set; }

    public Transform Parent => Entity.Data.Parent?.Transform;

    public Vector3 LocalForward { get => _localForward; set => _localForward = CheckDirty(value, _localForward); }
    private Vector3 _localForward = Vector3.UnitZ;
    public Vector3 LocalRight { get => _localRight; set => _localRight = CheckDirty(value, _localRight); }
    private Vector3 _localRight = Vector3.UnitX;
    public Vector3 LocalUp { get => _localUp; set => _localUp = CheckDirty(value, _localUp); }
    private Vector3 _localUp = Vector3.UnitY;
    public Vector3 LocalPosition { get => _localPosition; set => _localPosition = CheckDirty(value, _localPosition); }
    private Vector3 _localPosition = Vector3.Zero;
    public Vector3 LocalScale { get => _localScale; set => _localScale = CheckDirty(value, _localScale); }
    private Vector3 _localScale = Vector3.One;
    public Quaternion LocalRotation { get => _localRotation; set => SetQuaternion(CheckDirty(value, _localRotation)); }
    private Quaternion _localRotation = Quaternion.Identity;
    public Vector3 EulerAngles { get => _eulerAngles; set => SetEulerAngles(CheckDirty(value, _eulerAngles)); }
    private Vector3 _eulerAngles = Vector3.Zero;

    public Vector3 Forward => TransformVector3(LocalForward);
    public Vector3 Right => TransformVector3(LocalRight);
    public Vector3 Up => TransformVector3(LocalUp);
    public Vector3 Position => TransformVector3(LocalPosition);
    public Vector3 Scale => MultiplyVector3(LocalScale);
    public Quaternion Rotation => MultiplyQuaternion(_localRotation);

    public Matrix4x4 WorldMatrix => _worldMatrix;
    private Matrix4x4 _worldMatrix = Matrix4x4.Identity;

    private PerModelConstantBuffer _modelConstantBuffer;

    public override void OnRegister() =>
        TransformSystem.Register(this);

    public override void OnAwake() =>
        RecreateWorldMatrix();

    public void RecreateWorldMatrix()
    {
        CalculateOrientation();
        CalculateWorldMatrix();

        TransformChanged?.Invoke();
        foreach (var child in Entity.Data.Children)
            child.Transform.RecreateWorldMatrix();
    }

    public string GetString() =>
        $"{LocalPosition}\n{EulerAngles}\n{LocalScale}";

    internal PerModelConstantBuffer GetConstantBuffer()
    {
        // Transpose and set world matrix in constant buffer
        _modelConstantBuffer = new(Matrix4x4.Transpose(_worldMatrix));
        return _modelConstantBuffer;
    }
}

public sealed partial class Transform : EditorComponent, IHide
{
    private void CalculateOrientation()
    {
        // Calculate forward direction from Euler angles
        _localForward = CalculateForward(EulerAngles);

        // Calculate right and up directions
        _localRight = Vector3.Normalize(Vector3.Cross(LocalForward, Vector3.UnitY));
        _localUp = Vector3.Normalize(Vector3.Cross(LocalRight, LocalForward));
    }

    private Vector3 CalculateForward(Vector3 eulerAngles)
    {
        float sinX = MathF.Sin(eulerAngles.X.ToRadians());
        float cosX = MathF.Cos(eulerAngles.X.ToRadians());
        float sinY = MathF.Sin(eulerAngles.Y.ToRadians());
        float cosY = MathF.Cos(eulerAngles.Y.ToRadians());

        return Vector3.Normalize(new Vector3(sinY * cosX, -sinX, cosY * cosX));
    }

    private void CalculateWorldMatrix()
    {
        // Create transformation matrices
        var translationMatrix = Matrix4x4.CreateTranslation(LocalPosition);
        var rotationMatrix = Matrix4x4.CreateFromQuaternion(LocalRotation);
        var scaleMatrix = Matrix4x4.CreateScale(LocalScale);

        _worldMatrix = scaleMatrix * rotationMatrix * translationMatrix;

        // Apply parent transformation if available
        if (Parent is not null)
            _worldMatrix *= Parent.WorldMatrix;
    }
}

public sealed partial class Transform : EditorComponent, IHide
{
    private Vector3 CheckDirty(Vector3 newValue, Vector3 oldValue)
    {
        if (newValue != oldValue)
            RecreateWorldMatrix();

        return newValue;
    }

    private Quaternion CheckDirty(Quaternion newValue, Quaternion oldValue)
    {
        if (newValue != oldValue)
            RecreateWorldMatrix();

        return newValue;
    }
}

public sealed partial class Transform : EditorComponent, IHide
{
    private Vector3 TransformVector3(Vector3 local) =>
        Parent is not null ? Vector3.Transform(local, Parent.Rotation) + Parent.Position : local;

    private Vector3 MultiplyVector3(Vector3 local) =>
        Parent is not null ? Vector3.Multiply(local, Parent.Scale) : local;

    private Quaternion MultiplyQuaternion(Quaternion local) =>
        Parent is not null ? Quaternion.Multiply(local, Parent.Rotation) : local;

    private void SetQuaternion(Quaternion value)
    {
        _localRotation = value;
        _eulerAngles = value.ToEuler();
    }

    private void SetEulerAngles(Vector3 value)
    {
        _eulerAngles = value;
        _localRotation = value.ToRadians().FromEuler();
    }
}

public sealed partial class Transform : EditorComponent, IHide
{
    internal void SetPosition(float? x = null, float? y = null, float? z = null)
    {
        if (x.HasValue) _localPosition.X = x.Value;
        if (y.HasValue) _localPosition.Y = y.Value;
        if (z.HasValue) _localPosition.Z = z.Value;

        RecreateWorldMatrix();
    }

    internal void SetScale(float? x = null, float? y = null, float? z = null)
    {
        if (x.HasValue) _localScale.X = x.Value;
        if (y.HasValue) _localScale.Y = y.Value;
        if (z.HasValue) _localScale.Z = z.Value;

        RecreateWorldMatrix();
    }

    internal void SetEulerAngles(float? x = null, float? y = null, float? z = null)
    {
        if (x.HasValue) _eulerAngles.X = x.Value;
        if (y.HasValue) _eulerAngles.Y = y.Value;
        if (z.HasValue) _eulerAngles.Z = z.Value;

        _localRotation = _eulerAngles.ToRadians().FromEuler();

        RecreateWorldMatrix();
    }
}