using Engine;

var config = Config.GetDefault(
    title: "3D Engine Editor",
    width: 1280,
    height: 720);

new App(config)
    .AddPlugin(new DefaultPlugins())
    .AddPlugin(new UltralightPlugin("http://localhost:5000"))
    .Run();