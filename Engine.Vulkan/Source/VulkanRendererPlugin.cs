using System.Numerics;

namespace Engine;

/// <summary>Plugin that initializes the Vulkan renderer and drives it each Render stage.</summary>
public sealed class VulkanRendererPlugin : IPlugin
{
    private sealed class ClearColorExtract : IExtractSystem
    {
        public void Run(object appWorld, RenderWorld renderWorld)
        {
            if (appWorld is World w && w.TryResource<ClearColor>() is { } cc)
            {
                var v = cc.Value;
                renderWorld.Set(new RenderClearColor(v.X, v.Y, v.Z, v.W));
            }
        }
    }

    private sealed class CameraExtract : IExtractSystem
    {
        public void Run(object appWorld, RenderWorld renderWorld)
        {
            if (appWorld is not World w) return;
            var ecs = w.TryResource<EcsWorld>();
            if (ecs is null) return;
            var window = w.TryResource<AppWindow>();
            float aspect = 1f;
            if (window != null && window.Sdl.Height > 0)
                aspect = (float)window.Sdl.Width / window.Sdl.Height;
            var cameras = renderWorld.TryGet<RenderCameras>() ?? new RenderCameras();
            cameras.Items.Clear();
            // Iterate all Camera components
            foreach (var (entity, cam) in ecs.Query<Camera>())
            {
                Transform t = default;
                ecs.TryGet(entity, out t);
                var view = Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(t.Rotation)) * Matrix4x4.CreateTranslation(-t.Position);
                var proj = Matrix4x4.CreatePerspectiveFieldOfView(cam.FovY, aspect, cam.Near, cam.Far);
                cameras.Items.Add(new RenderCamera(view, proj));
            }
            renderWorld.Set(cameras);
        }
    }

    private sealed class MeshMaterialExtract : IExtractSystem
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

    private sealed class PreparePlaceholder : IPrepareSystem
    {
        public void Run(Engine.RenderWorld renderWorld, VulkanContext vk)
        {
            // TODO: Upload/allocate GPU buffers & descriptor sets once Vulkan backend is implemented.
        }
    }

    private sealed class QueuePlaceholder : IQueueSystem
    {
        public void Run(Engine.RenderWorld renderWorld, VulkanContext vk, CommandRecordingContext cmds)
        {
            // TODO: Record pipeline state & draw calls referencing prepared GPU resources.
        }
    }

    public void Build(App app)
    {
        var renderer = new Renderer();
        renderer.AddExtractSystem(new ClearColorExtract());
        renderer.AddExtractSystem(new CameraExtract());
        renderer.AddExtractSystem(new MeshMaterialExtract());
        renderer.AddPrepareSystem(new PreparePlaceholder());
        renderer.AddQueueSystem(new QueuePlaceholder());
        app.World.InsertResource(renderer);

        // Run Vulkan renderer after other Render stage systems (so UI etc. could be integrated later)
        app.AddSystem(Stage.Render, (world) =>
        {
            // Pass the App World to renderer; extraction systems can pull what they need.
            renderer.RenderFrame(world);
        });

        // Ensure disposal at app exit (Last stage)
        app.AddSystem(Stage.Last, (world) =>
        {
            if (world.TryResource<Renderer>() is { } r)
            {
                r.Dispose();
                world.RemoveResource<Renderer>();
            }
        });
    }
}
