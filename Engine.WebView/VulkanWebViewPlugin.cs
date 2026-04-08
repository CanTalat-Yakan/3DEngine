namespace Engine;

/// <summary>
/// Plugin that wires the Ultralight webview rendering into the Vulkan render graph.
/// Only activates when the graphics backend is Vulkan.
/// Mirrors the <see cref="VulkanImGuiPlugin"/> pattern.
/// </summary>
/// <remarks>
/// Selects between <see cref="WebViewRenderNode"/> (CPU bitmap surface mode) and
/// <see cref="GpuWebViewRenderNode"/> (GPU-accelerated mode) based on the
/// <see cref="WebViewInstance.Mode"/> configured by <see cref="WebViewPlugin"/>.
/// </remarks>
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

        Logger.Info("VulkanWebViewPlugin: Building - will add appropriate render node to Vulkan graph.");

        app.AddSystem(Stage.Startup, new SystemDescriptor(world =>
            {
                if (!world.TryGetResource<Renderer>(out var renderer))
                {
                    Logger.Warn("No Renderer resource found - WebView render node not added.");
                    return;
                }

                // Check if the WebViewInstance was initialized in GPU mode
                var webview = world.TryResource<WebViewInstance>();
                if (webview is not null && webview.Mode == WebViewMode.Gpu)
                {
                    renderer.AddNode(new GpuWebViewRenderNode());
                    Logger.Info("GpuWebViewRenderNode registered in render graph (GPU-accelerated mode).");
                }
                else
                {
                    renderer.AddNode(new WebViewRenderNode());
                    Logger.Info("WebViewRenderNode registered in render graph (CPU bitmap mode).");
                }
            }, "VulkanWebViewPlugin.Startup")
            .MainThreadOnly()
            .Read<WebViewInstance>()
            .Write<Renderer>());

        Logger.Info("VulkanWebViewPlugin: Build complete.");
    }
}

