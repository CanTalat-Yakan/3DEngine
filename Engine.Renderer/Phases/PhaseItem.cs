using System.Numerics;

namespace Engine;

/// <summary>
/// Marker interface for items in a <see cref="RenderPhase{T}"/>.
/// Each phase item represents a single draw call with a sort key, batch key, and draw function.
/// </summary>
/// <seealso cref="RenderPhase{T}"/>
/// <seealso cref="IDrawFunction{T}"/>
public interface IPhaseItem
{
    /// <summary>Sort key for depth ordering. Lower values render first in opaque (front-to-back);
    /// higher values render first in transparent (back-to-front).</summary>
    int SortKey { get; }

    /// <summary>Batch key for grouping items that share the same GPU resources (vertex buffer, pipeline).
    /// Items with the same batch key are drawn consecutively, reducing redundant state changes.</summary>
    int BatchKey { get; }
}

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

/// <summary>
/// Phase item for transparent 3D geometry. Sorted back-to-front by <see cref="SortKey"/>
/// within each <see cref="BatchKey"/> group to ensure correct alpha compositing.
/// </summary>
/// <seealso cref="Transparent3dPhase"/>
public struct TransparentPhaseItem : IPhaseItem
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
    public IDrawFunction<TransparentPhaseItem>? DrawFunction;
}

