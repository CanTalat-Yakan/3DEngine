using System.Numerics;

namespace Engine;

/// <summary>
/// Per-entity rendering data extracted from the game world during the extract phase.
/// Contains everything the prepare and queue phases need to upload vertex buffers and
/// issue draw calls, without requiring direct access to the ECS world.
/// </summary>
/// <seealso cref="MeshMaterialExtract"/>
/// <seealso cref="MeshPrepare"/>
/// <seealso cref="MainPassNode"/>
public sealed class ExtractedMeshData
{
    /// <summary>Extracted per-entity mesh rendering entry.</summary>
    /// <param name="EntityId">ECS entity ID for vertex buffer lookup in <see cref="MeshGpuRegistry"/>.</param>
    /// <param name="ModelMatrix">World-space model (object-to-world) matrix.</param>
    /// <param name="Albedo">Material albedo color (RGBA).</param>
    /// <param name="MeshData">Raw mesh data for GPU upload (if not already uploaded).</param>
    /// <param name="VertexCount">Number of vertices (cached for draw calls).</param>
    public readonly record struct Entry(int EntityId, Matrix4x4 ModelMatrix, Vector4 Albedo, Mesh MeshData, int VertexCount);

    /// <summary>List of mesh entries to render this frame.</summary>
    public List<Entry> Entries { get; } = new();

    /// <summary>Clears all entries for the next frame.</summary>
    public void Clear() => Entries.Clear();
}

