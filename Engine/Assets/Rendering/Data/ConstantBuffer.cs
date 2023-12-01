namespace Engine.Data;

public readonly struct ViewConstantBuffer(Matrix4x4 viewProjection, Vector3 cameraPosition)
{
    public readonly Matrix4x4 ViewProjection = viewProjection;
    public readonly Vector3 CameraPosition = cameraPosition;
}

public readonly struct PerModelConstantBuffer(Matrix4x4 modelView)
{
    public readonly Matrix4x4 ModelView = modelView;
}