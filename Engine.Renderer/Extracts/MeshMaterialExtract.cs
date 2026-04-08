using System.Numerics;

namespace Engine;

/// <summary>
/// Extracts entities with <see cref="Mesh"/> + <see cref="Material"/> components into
/// render entities with <see cref="RenderMeshInstance"/> components in the render world's
/// <see cref="EcsWorld"/>.
/// </summary>
/// <remarks>
/// Bevy equivalent: <c>extract_meshes</c> system that spawns render entities with
/// <c>RenderMeshInstance</c> + <c>MeshTransforms</c> components.
/// </remarks>
/// <seealso cref="RenderMeshInstance"/>
/// <seealso cref="QueueMeshPhaseItems"/>
public sealed class MeshMaterialExtract : IExtractSystem
{
    /// <inheritdoc />
    public void Run(World world, RenderWorld renderWorld)
    {
        if (!world.TryGetResource<EcsWorld>(out var ecs)) return;

        foreach (var (entity, mesh) in ecs.Query<Mesh>())
        {
            if (!ecs.TryGet(entity, out Material mat)) continue;
            if (mesh.Positions is null || mesh.Positions.Length == 0) continue;

            // Compute model matrix from Transform (identity if missing)
            Transform t = default;
            ecs.TryGet(entity, out t);

            var model = Matrix4x4.CreateScale(t.Scale)
                      * Matrix4x4.CreateFromQuaternion(t.Rotation)
                      * Matrix4x4.CreateTranslation(t.Position);

            // Spawn render entity with RenderMeshInstance component
            int renderEntity = renderWorld.Spawn();
            renderWorld.Entities.Add(renderEntity, new RenderMeshInstance
            {
                MainEntityId = entity,
                ModelMatrix = model,
                Albedo = mat.Albedo,
                MeshData = mesh,
                VertexCount = mesh.Positions.Length
            });
        }
    }
}