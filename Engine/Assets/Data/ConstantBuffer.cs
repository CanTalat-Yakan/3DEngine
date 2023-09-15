namespace Engine.Data;

internal struct ViewConstantBuffer
{
    public Matrix4x4 ViewProjection;
    public Vector3 CameraPosition;
}

internal struct PerModelConstantBuffer
{
    public Matrix4x4 ModelView;
}

//internal struct DirectionalLightConstantBuffer
//{
//    public Vector3 Direction;
//    public float Pad;
//    public Vector4 Diffuse;
//    public Vector4 Ambient;
//    public float Intensity;
//    public Vector3 Pad2;
//}

//internal struct PointLightConstantBuffer
//{
//    public Vector3 Position;
//    public float Pad;
//    public Vector4 Diffuse;
//    public float Intensity;
//    public float Radius;
//    public Vector2 Pad2;
//}
