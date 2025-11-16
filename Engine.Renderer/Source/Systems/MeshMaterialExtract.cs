namespace Engine;

public sealed class MeshMaterialExtract : IExtractSystem
{
    public void Run(object appWorld, Engine.RenderWorld renderWorld)
    {
        if (appWorld is not World w) return;
        var ecs = w.TryResource<EcsWorld>();
        if (ecs is null) return;
        var drawLists = renderWorld.TryGet<RenderDrawLists>() ?? new RenderDrawLists();
        drawLists.Clear();
        // Build opaque draw list (very naive) for entities that have Mesh + Material
        foreach (var (entity, mesh) in ecs.Query<Mesh>())
        {
            if (!ecs.TryGet(entity, out Material mat)) continue;
            // SortKey simplistic: vertex count
            int sortKey = mesh.Positions?.Length ?? 0;
            drawLists.Opaque.Add(new DrawCommand(entity, sortKey));
        }
        // Simple sort (front-load smaller meshes)
        drawLists.Opaque.Sort((a,b) => a.SortKey.CompareTo(b.SortKey));
        renderWorld.Set(drawLists);
    }
}