using System.Numerics;

namespace Engine;

/// <summary>Extracts camera data from the app world into the render world for the sample pipeline.</summary>
public sealed class SampleExtract : IExtractSystem
{
    public void Run(object appWorld, RenderWorld renderWorld)
    {
        if (appWorld is not World w) return;
        var ecs = w.TryResource<EcsWorld>();
        if (ecs is null) return;

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