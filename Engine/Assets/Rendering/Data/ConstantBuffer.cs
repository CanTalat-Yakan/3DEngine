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

//public struct DirectionalLightConstantBuffer
//{
//    public Vector3 Direction;
//    public float Pad;
//    public Vector4 Diffuse;
//    public Vector4 Ambient;
//    public float Intensity;
//}

//public struct PointLightConstantBuffer
//{
//    public Vector3 Position;
//    public float Pad;
//    public Vector4 Diffuse;
//    public float Intensity;
//    public float Radius;
//}
