using System.Numerics;

namespace Engine;

/// <summary>
/// Per-entity render component attached to render entities during the extract phase.
/// Combines the model matrix, material albedo, mesh data, and source entity reference
/// needed by the prepare and queue phases.
/// Bevy equivalent: <c>RenderMeshInstance</c> + <c>MeshTransforms</c>.
/// </summary>
/// <seealso cref="MeshMaterialExtract"/>
/// <seealso cref="MeshPrepare"/>
/// <seealso cref="QueueMeshPhaseItems"/>
public struct RenderMeshInstance
{
    /// <summary>The source game-world entity ID (for <see cref="MeshGpuRegistry"/> vertex buffer lookup).</summary>
    public int MainEntityId;

    /// <summary>Object-to-world model matrix.</summary>
    public Matrix4x4 ModelMatrix;

    /// <summary>Material albedo color (RGBA).</summary>
    public Vector4 Albedo;

    /// <summary>Raw mesh data for GPU upload (vertex positions).</summary>
    public Mesh MeshData;

    /// <summary>Number of vertices (cached for draw calls).</summary>
    public int VertexCount;
}

/// <summary>
/// Per-entity render component for cameras extracted during the extract phase.
/// Contains the computed view/projection matrices and viewport dimensions.
/// Bevy equivalent: <c>ExtractedView</c>.
/// </summary>
/// <seealso cref="CameraExtract"/>
/// <seealso cref="MainPassNode"/>
public struct ExtractedView
{
    /// <summary>View (world-to-eye) matrix.</summary>
    public Matrix4x4 View;

    /// <summary>Projection (eye-to-clip) matrix.</summary>
    public Matrix4x4 Projection;

    /// <summary>Viewport width in pixels.</summary>
    public int Width;

    /// <summary>Viewport height in pixels.</summary>
    public int Height;
}

