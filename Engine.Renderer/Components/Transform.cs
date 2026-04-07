using System.Numerics;

namespace Engine;

/// <summary>World-space transform component (position, rotation, scale).</summary>
/// <seealso cref="Camera"/>
/// <seealso cref="Mesh"/>
public struct Transform
{
    /// <summary>Position in world space.</summary>
    public Vector3 Position;

    /// <summary>Rotation as a quaternion.</summary>
    public Quaternion Rotation;

    /// <summary>Scale factor per axis.</summary>
    public Vector3 Scale;

    /// <summary>Creates a transform at the specified position with identity rotation and unit scale.</summary>
    /// <param name="position">World-space position.</param>
    public Transform(Vector3 position)
    {
        Position = position;
        Rotation = Quaternion.Identity;
        Scale = Vector3.One;
    }
}