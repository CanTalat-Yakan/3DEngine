using System.Numerics;

namespace WinUI3DEngine.Assets.Engine.Data
{
    public struct SViewConstantsBuffer
    {
        public Matrix4x4 ViewProjection;
        public Vector3 CameraPisiton;
        public float pad;
    }

    public struct SPerModelConstantBuffer
    {
        public Matrix4x4 ModelView;
    }

    public struct SDirectionalLightConstantBuffer
    {
        public Vector3 Direction;
        public float pad;
        public Vector4 Diffuse;
        public Vector4 Ambient;
        public float Intensity;
        public Vector3 pad2;
    }

    public struct SPointLightConstantBuffer
    {
        public Vector3 Position;
        public float pad;
        public Vector4 Diffuse;
        public float Intensity;
        public float Radius;
        public Vector2 pad2;
    }
}
