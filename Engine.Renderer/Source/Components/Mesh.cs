using System.Numerics;

namespace Engine;

/// <summary>Mesh component containing raw position data (placeholder for real GPU buffers).</summary>
public struct Mesh
{
    public Vector3[] Positions;
    public Mesh(Vector3[] positions) { Positions = positions; }
}