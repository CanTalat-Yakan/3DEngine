namespace Engine.Data;

public struct ViewConstantBuffer
{
    public Matrix4x4 ViewProjection;
    public Vector3 CameraPosition;
}

public struct PerModelConstantBuffer
{
    public Matrix4x4 ModelView;
}