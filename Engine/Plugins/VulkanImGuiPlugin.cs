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

        ImGuiRenderNode? imguiNode = null;

        app.AddSystem(Stage.Startup, (World world) =>
        {
            var renderer = world.TryResource<Renderer>();
            if (renderer is null)
            {
                Logger.Warn("No Renderer resource found — ImGui render node not added.");
                return;
            }

            imguiNode = new ImGuiRenderNode();
            renderer.AddNode(imguiNode);
            Logger.Info("ImGuiRenderNode registered in render graph (depends on 'sample' node).");
        });

        app.AddSystem(Stage.Cleanup, (World world) =>
        {
            Logger.Info("VulkanImGuiPlugin: Cleanup — disposing ImGui render node resources...");
            imguiNode?.Dispose();
            Logger.Info("VulkanImGuiPlugin: Cleanup complete.");
        });

        Logger.Info("VulkanImGuiPlugin: Build complete.");
    }
}

