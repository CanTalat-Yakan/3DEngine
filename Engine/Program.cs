using Engine;

var config = Config.GetDefault(
    title: "3D Engine",
    width: 1280,
    height: 720,
    graphics: GraphicsBackend.Vulkan);

new App(config)
    .AddPlugin(new DefaultPlugins())
    .AddPlugin(new BrowserPlugin { InitialUrl = "https://example.com" })
    .AddPlugin(new VulkanBrowserPlugin())
    .Run();
