using Vortice.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice
{
    private partial void CreateSyncObjects()
    {
        _imageAvailableSemaphores = new VkSemaphore[MaxFramesInFlight];
        _renderFinishedSemaphores = new VkSemaphore[MaxFramesInFlight];
        _inFlightFences = new VkFence[MaxFramesInFlight];

        VkFenceCreateInfo fenceInfo = new() { flags = VkFenceCreateFlags.Signaled };

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            _deviceApi.vkCreateSemaphore(_device, out _imageAvailableSemaphores[i]).CheckResult();
            _deviceApi.vkCreateSemaphore(_device, out _renderFinishedSemaphores[i]).CheckResult();
            _deviceApi.vkCreateFence(_device, &fenceInfo, null, out _inFlightFences[i]).CheckResult();
        }
    }

    private partial void DestroySyncObjects()
    {
        foreach (var fence in _inFlightFences)
            if (fence.Handle != 0) _deviceApi.vkDestroyFence(_device, fence);
        foreach (var sem in _imageAvailableSemaphores)
            if (sem.Handle != 0) _deviceApi.vkDestroySemaphore(_device, sem);
        foreach (var sem in _renderFinishedSemaphores)
            if (sem.Handle != 0) _deviceApi.vkDestroySemaphore(_device, sem);

        _inFlightFences = Array.Empty<VkFence>();
        _imageAvailableSemaphores = Array.Empty<VkSemaphore>();
        _renderFinishedSemaphores = Array.Empty<VkSemaphore>();
    }
}
