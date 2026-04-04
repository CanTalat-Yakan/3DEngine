namespace Engine;

/// <summary>
/// Plugin that wires ImGui rendering into the Vulkan render graph.
/// Only activates when the graphics backend is Vulkan.
/// </summary>
public sealed class VulkanImGuiPlugin : IPlugin
{
    private static readonly ILogger Logger = Log.Category("Engine.ImGui.Vulkan");

    public void Build(App app)
    {
        var cfg = app.World.Resource<Config>();
        if (cfg.Graphics != GraphicsBackend.Vulkan)
        {
            Logger.Info("VulkanImGuiPlugin: Non-Vulkan backend — skipping.");
            return;
        }

        Logger.Info("VulkanImGuiPlugin: Building — will add ImGui render node to Vulkan graph.");

        app.AddSystem(Stage.Startup, (World world) =>
        {
            if (!world.TryGetResource<Renderer>(out var renderer))
            {
                Logger.Warn("No Renderer resource found — ImGui render node not added.");
                return;
            }

            renderer.AddNode(new ImGuiRenderNode());
            Logger.Info("ImGuiRenderNode registered in render graph.");
        });

        Logger.Info("VulkanImGuiPlugin: Build complete.");
    }
}

