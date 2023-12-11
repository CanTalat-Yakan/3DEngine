namespace Engine.Data;

public struct ViewConstantBuffer(Matrix4x4 viewProjection, Vector3 cameraPosition)
{
    public Matrix4x4 ViewProjection = viewProjection;
    public Vector3 CameraPosition = cameraPosition;
}

public struct PerModelConstantBuffer(Matrix4x4 modelView)
{
    public Matrix4x4 ModelView = modelView;
}