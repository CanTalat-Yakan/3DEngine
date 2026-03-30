using Vortice.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice
{
    private partial void CreateSurface()
    {
        var handle = _surfaceSource!.CreateSurfaceHandle((nint)_instance.Handle);
        _surface = new VkSurfaceKHR((ulong)handle);
    }

    private partial void DestroySurface()
    {
        if (_surface.Handle != 0)
        {
            _instanceApi.vkDestroySurfaceKHR(_instance, _surface);
            _surface = default;
        }
    }
}
