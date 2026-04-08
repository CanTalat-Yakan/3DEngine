using System.Numerics;

namespace Engine;

/// <summary>
/// Extracts entities with <see cref="Mesh"/> + <see cref="Material"/> components into
/// <see cref="RenderDrawLists"/> sorted by vertex count for front-to-back rendering,
/// and populates <see cref="ExtractedMeshData"/> with per-entity rendering data
/// (model matrix, albedo, mesh positions) for the prepare and queue phases.
/// </summary>
/// <seealso cref="RenderDrawLists"/>
/// <seealso cref="ExtractedMeshData"/>
/// <seealso cref="DrawCommand"/>
public sealed class MeshMaterialExtract : IExtractSystem
{
    /// <inheritdoc />
    public void Run(World world, RenderWorld renderWorld)
    {
        if (!world.TryGetResource<EcsWorld>(out var ecs)) return;

        var drawLists = renderWorld.TryGet<RenderDrawLists>() ?? new RenderDrawLists();
        drawLists.Clear();

        var extracted = renderWorld.TryGet<ExtractedMeshData>() ?? new ExtractedMeshData();
        extracted.Clear();

        foreach (var (entity, mesh) in ecs.Query<Mesh>())
        {
            if (!ecs.TryGet(entity, out Material mat)) continue;
            if (mesh.Positions is null || mesh.Positions.Length == 0) continue;

            int sortKey = mesh.Positions.Length;
            drawLists.Opaque.Add(new DrawCommand(entity, sortKey));

            // Compute model matrix from Transform (identity if missing)
            Transform t = default;
            ecs.TryGet(entity, out t);

            var model = Matrix4x4.CreateScale(t.Scale)
                      * Matrix4x4.CreateFromQuaternion(t.Rotation)
                      * Matrix4x4.CreateTranslation(t.Position);

            extracted.Entries.Add(new ExtractedMeshData.Entry(
                entity, model, mat.Albedo, mesh, mesh.Positions.Length));
        }

        drawLists.Opaque.Sort((a, b) => a.SortKey.CompareTo(b.SortKey));
        renderWorld.Set(drawLists);
        renderWorld.Set(extracted);
    }
}