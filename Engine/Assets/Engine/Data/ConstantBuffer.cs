namespace Engine.Data;

internal struct SViewConstantBuffer
{
    public Matrix4x4 ViewProjection;
    public Vector3 CameraPositon;
    public float Pad;
}

internal struct SPerModelConstantBuffer
{
    public Matrix4x4 ModelView;
}

internal struct SDirectionalLightConstantBuffer
{
    public Vector3 Direction;
    public float Pad;
    public Vector4 Diffuse;
    public Vector4 Ambient;
    public float Intensity;
    public Vector3 Pad2;
}

internal struct SPointLightConstantBuffer
{
    public Vector3 Position;
    public float Pad;
    public Vector4 Diffuse;
    public float Intensity;
    public float Radius;
    public Vector2 Pad2;
}
