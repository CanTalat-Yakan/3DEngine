using System.Numerics;

namespace Engine;

/// <summary>
/// Phase item for opaque 3D geometry. Sorted front-to-back by <see cref="SortKey"/>
/// within each <see cref="BatchKey"/> group to maximise early-Z rejection while
/// minimising state changes.
/// </summary>
/// <seealso cref="Opaque3dPhase"/>
public struct OpaquePhaseItem : IPhaseItem
{
    /// <summary>ECS entity ID for GPU resource lookup in <see cref="MeshGpuRegistry"/>.</summary>
    public int EntityId;

    /// <inheritdoc />
    public int SortKey { get; set; }

    /// <inheritdoc />
    public int BatchKey { get; set; }

    /// <summary>Object-to-world model matrix.</summary>
    public Matrix4x4 ModelMatrix;

    /// <summary>Material albedo color (RGBA).</summary>
    public Vector4 Albedo;

    /// <summary>Number of vertices to draw.</summary>
    public int VertexCount;

    /// <summary>The draw function that knows how to render this item.</summary>
    public IDrawFunction<OpaquePhaseItem>? DrawFunction;
}

