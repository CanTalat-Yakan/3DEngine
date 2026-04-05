namespace Engine;

/// <summary>Extracts Mesh+Material entities into opaque draw lists for the renderer.</summary>
public sealed class MeshMaterialExtract : IExtractSystem
{
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