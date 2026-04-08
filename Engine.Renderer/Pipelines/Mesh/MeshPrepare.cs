namespace Engine;

/// <summary>
/// Prepare system that ensures GPU vertex buffers exist for all mesh entities
/// in the current frame's <see cref="ExtractedMeshData"/>.
/// Uses <see cref="MeshGpuRegistry"/> to cache buffers across frames.
/// </summary>
/// <seealso cref="ExtractedMeshData"/>
/// <seealso cref="MeshGpuRegistry"/>
/// <seealso cref="MainPassNode"/>
public sealed class MeshPrepare : IPrepareSystem
{
    /// <inheritdoc />
    /// <param name="renderWorld">The render world containing <see cref="ExtractedMeshData"/>.</param>
    /// <param name="renderContext">Render context providing GPU device access.</param>
    public void Run(RenderWorld renderWorld, RenderContext renderContext)
    {
        var extracted = renderWorld.TryGet<ExtractedMeshData>();
        if (extracted is null || extracted.Entries.Count == 0)
            return;

        var registry = renderWorld.TryGet<MeshGpuRegistry>();
        if (registry is null)
        {
            registry = new MeshGpuRegistry();
            renderWorld.Set(registry);
        }

        var gfx = renderContext.Device;

        foreach (var entry in extracted.Entries)
        {
            // GetOrCreate checks if a buffer already exists; creates and uploads only if missing.
            registry.GetOrCreate(entry.EntityId, entry.MeshData, gfx);
        }
    }
}

