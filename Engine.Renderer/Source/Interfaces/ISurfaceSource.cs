using Vortice.Vulkan;

namespace Engine;

public interface ISurfaceSource
{
    // Extension names SDL/platform requires for instance creation
    IReadOnlyList<string> GetRequiredInstanceExtensions();
    // Create surface for the given instance
    VkSurfaceKHR CreateSurface(VkInstance instance);
    // Current drawable size (used for swapchain extent)
    (uint Width, uint Height) GetDrawableSize();
}