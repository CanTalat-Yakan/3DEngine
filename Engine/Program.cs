using System.Numerics;
using Engine;

var config = Config.GetDefault(
    title: "3D Engine",
    width: 1280,
    height: 720,
    graphics: GraphicsBackend.Vulkan);

new App(config)
    .AddPlugin(new DefaultPlugins())
    .AddPlugin(new WebViewPlugin())
    .Run();

[Behavior]
public struct TriangleMeshTest
{
    [OnStartup]
    public static void Start(BehaviorContext ctx)
    {
        var world = ctx.World;
        var ecs = world.Resource<EcsWorld>();
        var e = ecs.Spawn();
        ecs.Add(e, new Camera(fovYDegrees: 60f, near: 0.1f, far: 1000f));
        ecs.Add(e, new Transform(new Vector3(0, 0, 5)));

        var mesh = ecs.Spawn();
        ecs.Add(mesh, new Mesh(new[] { new Vector3(0,1,0), new Vector3(-1,-1,0), new Vector3(1,-1,0) }));
        ecs.Add(mesh, new Material(new Vector4(1, 1, 1, 1))); // white triangle
        ecs.Add(mesh, new Transform(Vector3.Zero));
    }
}