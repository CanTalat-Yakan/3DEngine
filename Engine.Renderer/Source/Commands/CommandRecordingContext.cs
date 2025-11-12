using Vortice.Vulkan;

namespace Engine;

public sealed class CommandRecordingContext
{
    // Minimal Vulkan handles required to record into the current swapchain image
    public VkCommandBuffer CommandBuffer { get; internal set; }
    public VkExtent2D SwapchainExtent { get; internal set; }
    public VkRenderPass RenderPass { get; internal set; }
    public VkFramebuffer Framebuffer { get; internal set; }
}