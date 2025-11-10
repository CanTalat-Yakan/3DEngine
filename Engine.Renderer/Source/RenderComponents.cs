using System.Numerics;

namespace Engine;

/// <summary>World-space transform (position, rotation, scale).</summary>
public struct Transform
{
    public Vector3 Position;
    public Quaternion Rotation;
    public Vector3 Scale;
    public Transform(Vector3 position)
    { Position = position; Rotation = Quaternion.Identity; Scale = Vector3.One; }
}

/// <summary>Simple perspective camera marker with projection parameters.</summary>
public struct Camera
{
    public float FovY; // radians
    public float Near;
    public float Far;
    public Camera(float fovY = 60f * (float)(Math.PI / 180.0), float near = 0.1f, float far = 1000f)
    { FovY = fovY; Near = near; Far = far; }
}

/// <summary>Mesh component containing raw position data (placeholder for real GPU buffers).</summary>
public struct Mesh
{
    public Vector3[] Positions;
    public Mesh(Vector3[] positions) { Positions = positions; }
}

/// <summary>Simple material with RGBA albedo color.</summary>
public struct Material
{
    public Vector4 Albedo;
    public Material(Vector4 albedo) { Albedo = albedo; }
}
