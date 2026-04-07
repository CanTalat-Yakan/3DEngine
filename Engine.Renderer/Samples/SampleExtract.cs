using System.Numerics;

namespace Engine;

/// <summary>Extracts camera data from the world into the render world for the sample pipeline.</summary>
/// <remarks>
/// Queries all entities with a <see cref="Camera"/> component, computes view and perspective
/// projection matrices using the entity's <see cref="Transform"/>, and writes a
/// <see cref="RenderCameras"/> collection into the render world for consumption by
/// <see cref="SamplePrepare"/> and <see cref="SampleQueue"/>.
/// </remarks>
/// <seealso cref="SamplePrepare"/>
/// <seealso cref="SampleQueue"/>
/// <seealso cref="RenderCameras"/>
public sealed class SampleExtract : IExtractSystem
{
    /// <inheritdoc />
    /// <param name="world">The main application world containing ECS data.</param>
    /// <param name="renderWorld">The render world to write extracted camera data into.</param>
    public void Run(World world, RenderWorld renderWorld)
    {
        if (!world.TryGetResource<EcsWorld>(out var ecs)) return;

        var surface = renderWorld.TryGet<RenderSurfaceInfo>();
        int surfaceW = surface?.Width > 0 ? surface!.Width : 1;
        int surfaceH = surface?.Height > 0 ? surface!.Height : 1;

        var cameras = renderWorld.TryGet<RenderCameras>() ?? new RenderCameras();
        cameras.Items.Clear();

        foreach (var (entity, cam) in ecs.Query<Camera>())
        {
            float aspect = surfaceH > 0 ? (float)surfaceW / surfaceH : 1f;

            Transform t = default;
            ecs.TryGet(entity, out t);

            var view = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(t.Rotation))
                     * Matrix4x4.CreateTranslation(-t.Position);
            var proj = Matrix4x4.CreatePerspectiveFieldOfView(cam.FovY, aspect, cam.Near, cam.Far);

            cameras.Items.Add(new RenderCamera(view, proj, surfaceW, surfaceH));
        }

        renderWorld.Set(cameras);
    }
}