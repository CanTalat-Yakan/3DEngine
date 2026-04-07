namespace Engine;

/// <summary>
/// Plugin that wires the Ultralight webview rendering into the Vulkan render graph.
/// Only activates when the graphics backend is Vulkan.
/// Mirrors the <see cref="VulkanImGuiPlugin"/> pattern.
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
                    Logger.Warn("No Renderer resource found - WebViewRenderNode not added.");
                    return;
                }
            
                renderer.AddNode(new WebViewRenderNode());
                Logger.Info("WebViewRenderNode registered in render graph.");
            }, "VulkanWebViewPlugin.Startup")
            .MainThreadOnly()
            .Write<Renderer>());

        Logger.Info("VulkanWebViewPlugin: Build complete.");
    }
}

