namespace Engine;

/// <summary>
/// Plugin that wires the Ultralight browser rendering into the Vulkan render graph.
/// Only activates when the graphics backend is Vulkan.
/// Mirrors the <see cref="VulkanImGuiPlugin"/> pattern.
/// </summary>
public sealed class VulkanBrowserPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.Browser.Vulkan");

    public void Build(App app)
    {
        var cfg = app.World.Resource<Config>();
        if (cfg.Graphics != GraphicsBackend.Vulkan)
        {
            Logger.Info("VulkanBrowserPlugin: Non-Vulkan backend — skipping.");
            return;
        }

        Logger.Info("VulkanBrowserPlugin: Building — will add BrowserRenderNode to Vulkan graph.");

        app.AddSystem(Stage.Startup, (World world) =>
        {
            var renderer = world.TryResource<Renderer>();
            if (renderer is null)
            {
                Logger.Warn("No Renderer resource found — BrowserRenderNode not added.");
                return;
            }

            renderer.AddNode(new BrowserRenderNode());
            Logger.Info("BrowserRenderNode registered in render graph.");
        });

        Logger.Info("VulkanBrowserPlugin: Build complete.");
    }
}

