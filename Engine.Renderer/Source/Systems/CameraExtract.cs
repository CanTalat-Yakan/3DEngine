using System.Numerics;

namespace Engine;

public sealed class CameraExtract : IExtractSystem
{
    public void Run(object appWorld, RenderWorld renderWorld)
    {
        if (appWorld is not World w) return;
        var ecs = w.TryResource<EcsWorld>();
        if (ecs is null) return;

        // Primary surface size provided by the backend plugin (SDL, Avalonia, etc.)
        var surface = renderWorld.TryGet<RenderSurfaceInfo>();
        int surfaceW = surface?.Width > 0 ? surface!.Width : 1;
        int surfaceH = surface?.Height > 0 ? surface!.Height : 1;
        var textures = renderWorld.TryGet<RenderTextures>();

        var cameras = renderWorld.TryGet<RenderCameras>() ?? new RenderCameras();
        cameras.Items.Clear();

        foreach (var (entity, cam) in ecs.Query<global::Engine.Camera>())
        {
            // Resolve render target size: render texture if set and found, else primary surface
            int wPixels = surfaceW, hPixels = surfaceH;
            if (!string.IsNullOrEmpty(cam.TargetName) && textures != null && textures.TryGet(cam.TargetName!, out var desc))
            {
                wPixels = Math.Max(1, desc.Width);
                hPixels = Math.Max(1, desc.Height);
            }

            float aspect = hPixels > 0 ? (float)wPixels / hPixels : 1f;

            Transform t = default;
            ecs.TryGet(entity, out t);
            var view = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(t.Rotation)) * Matrix4x4.CreateTranslation(-t.Position);
            var proj = Matrix4x4.CreatePerspectiveFieldOfView(cam.FovY, aspect, cam.Near, cam.Far);
            cameras.Items.Add(new RenderCamera(view, proj, wPixels, hPixels));
        }
        renderWorld.Set(cameras);
    }
}
