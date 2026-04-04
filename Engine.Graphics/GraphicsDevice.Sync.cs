using Vortice.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice
{
    private partial void CreateSyncObjects()
    {
        Logger.Debug($"Creating synchronization objects for {MaxFramesInFlight} frames-in-flight...");
        _imageAvailableSemaphores = new VkSemaphore[MaxFramesInFlight];
        _renderFinishedSemaphores = new VkSemaphore[MaxFramesInFlight];
        _inFlightFences = new VkFence[MaxFramesInFlight];

        VkFenceCreateInfo fenceInfo = new() { flags = VkFenceCreateFlags.Signaled };

        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            _deviceApi.vkCreateSemaphore(out _imageAvailableSemaphores[i]).CheckResult();
            _deviceApi.vkCreateSemaphore(out _renderFinishedSemaphores[i]).CheckResult();
            _deviceApi.vkCreateFence(&fenceInfo, null, out _inFlightFences[i]).CheckResult();
        }
        Logger.Debug($"Sync objects created: {MaxFramesInFlight} semaphore pairs + {MaxFramesInFlight} fences (pre-signaled).");
    }

    private partial void DestroySyncObjects()
    {
        Logger.Debug("Destroying sync objects (fences and semaphores)...");
        foreach (var fence in _inFlightFences)
            if (fence.Handle != 0) _deviceApi.vkDestroyFence(fence);
        foreach (var sem in _imageAvailableSemaphores)
            if (sem.Handle != 0) _deviceApi.vkDestroySemaphore(sem);
        foreach (var sem in _renderFinishedSemaphores)
            if (sem.Handle != 0) _deviceApi.vkDestroySemaphore(sem);

        _inFlightFences = Array.Empty<VkFence>();
        _imageAvailableSemaphores = Array.Empty<VkSemaphore>();
        _renderFinishedSemaphores = Array.Empty<VkSemaphore>();
        Logger.Debug("Sync objects destroyed.");
    }
}
