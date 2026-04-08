namespace Engine;

/// <summary>
/// Plugin that wires the Ultralight webview rendering into the Vulkan render graph.
/// Only activates when the graphics backend is Vulkan.
/// Uses the CPU bitmap <see cref="WebViewRenderNode"/>.
/// </summary>
public sealed class VulkanWebViewPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView.Vulkan");

    /// <inheritdoc />
    public void Build(App app)
    {
        var cfg = app.World.Resource<Config>();
        if (cfg.Graphics != GraphicsBackend.Vulkan)
        {
            Logger.Info("VulkanWebViewPlugin: Non-Vulkan backend - skipping.");
            return;
        }

        Logger.Info("VulkanWebViewPlugin: Building - will add WebViewRenderNode to Vulkan graph.");

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
            {
                if (!world.TryGetResource<Renderer>(out var renderer))
                {
                    Logger.Warn("No Renderer resource found - WebView render node not added.");
                    return;
                }

                renderer.Graph.AddNode("webview", new WebViewRenderNode());
                renderer.Graph.AddNodeEdge("main_pass", "webview");
                Logger.Info("WebViewRenderNode registered in render graph (CPU bitmap mode, after 'sample').");
            }, "VulkanWebViewPlugin.Startup")
            .MainThreadOnly()
            .Read<WebViewInstance>()
            .Write<Renderer>());

        Logger.Info("VulkanWebViewPlugin: Build complete.");
    }
}
