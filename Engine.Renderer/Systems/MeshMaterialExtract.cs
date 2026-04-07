namespace Engine;

/// <summary>
/// Extracts entities with <see cref="Mesh"/> + <see cref="Material"/> components into
/// <see cref="RenderDrawLists"/> sorted by vertex count for front-to-back rendering.
/// </summary>
/// <seealso cref="RenderDrawLists"/>
/// <seealso cref="DrawCommand"/>
public sealed class MeshMaterialExtract : IExtractSystem
{
    /// <inheritdoc />
    public void Run(World world, RenderWorld renderWorld)
    {
        if (!world.TryGetResource<EcsWorld>(out var ecs)) return;
        var drawLists = renderWorld.TryGet<RenderDrawLists>() ?? new RenderDrawLists();
        drawLists.Clear();

        foreach (var (entity, mesh) in ecs.Query<Mesh>())
        {
            if (!ecs.TryGet(entity, out Material mat)) continue;
            int sortKey = mesh.Positions?.Length ?? 0;
            drawLists.Opaque.Add(new DrawCommand(entity, sortKey));
        }

        drawLists.Opaque.Sort((a, b) => a.SortKey.CompareTo(b.SortKey));
        renderWorld.Set(drawLists);
    }
}