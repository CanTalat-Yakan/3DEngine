using Engine;

var config = Config.GetDefault(
    title: "3D Engine Editor",
    width: 1280,
    height: 720,
    graphics: GraphicsBackend.Vulkan);

new App(config)
    .AddPlugin(new DefaultPlugins())
    .Run();
