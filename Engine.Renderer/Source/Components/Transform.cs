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