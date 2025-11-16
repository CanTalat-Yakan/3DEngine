using Vortice.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice
{
    private struct QueueFamilyIndices
    {
        public uint Graphics;
        public uint Present;
        public bool IsComplete => Graphics != uint.MaxValue && Present != uint.MaxValue;
    }

    private partial void SelectPhysicalDevice()
    {
        _instanceApi.vkEnumeratePhysicalDevices(_instance, out uint deviceCount).CheckResult();
        if (deviceCount == 0) throw new InvalidOperationException("No Vulkan physical devices.");

        Span<VkPhysicalDevice> devices = stackalloc VkPhysicalDevice[(int)deviceCount];
        _instanceApi.vkEnumeratePhysicalDevices(_instance, devices).CheckResult();

        VkPhysicalDevice? best = null;
        var bestScore = int.MinValue;

        foreach (var device in devices)
        {
            var indices = FindQueueFamilies(device);
            if (!indices.IsComplete) continue;

            _instanceApi.vkGetPhysicalDeviceProperties(device, out var props);
            _instanceApi.vkGetPhysicalDeviceFeatures(device, out var features);

            if (!features.geometryShader) continue;

            var score = props.deviceType switch
            {
                VkPhysicalDeviceType.DiscreteGpu => 1000,
                VkPhysicalDeviceType.IntegratedGpu => 500,
                _ => 100
            };

            score += (int)props.limits.maxImageDimension2D;

            if (score > bestScore)
            {
                bestScore = score;
                best = device;
                _graphicsQueueFamily = indices.Graphics;
                _presentQueueFamily = indices.Present;
                _adapterInfo = new GraphicsAdapterInfo(
                    Utf8(new ReadOnlySpan<byte>(props.deviceName, MaxPhysicalDeviceNameSize)),
                    props.vendorID,
                    props.deviceID,
                    ToDeviceType(props.deviceType));
            }
        }

        if (best is null)
            throw new InvalidOperationException("Failed to find a suitable GPU for Vulkan.");

        _physicalDevice = best.Value;
    }

    private QueueFamilyIndices FindQueueFamilies(VkPhysicalDevice device)
    {
        var result = new QueueFamilyIndices { Graphics = uint.MaxValue, Present = uint.MaxValue };

        _instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(device, out uint count);
        Span<VkQueueFamilyProperties> props = stackalloc VkQueueFamilyProperties[(int)count];
        _instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(device, props);

        for (uint i = 0; i < count; i++)
        {
            if ((props[(int)i].queueFlags & VkQueueFlags.Graphics) != 0)
                result.Graphics = i;

            _instanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(device, i, _surface, out VkBool32 supports);
            if (supports)
                result.Present = i;

            if (result.IsComplete) break;
        }

        return result;
    }

    private static GraphicsDeviceType ToDeviceType(VkPhysicalDeviceType type) => type switch
    {
        VkPhysicalDeviceType.DiscreteGpu => GraphicsDeviceType.DiscreteGpu,
        VkPhysicalDeviceType.IntegratedGpu => GraphicsDeviceType.IntegratedGpu,
        VkPhysicalDeviceType.VirtualGpu => GraphicsDeviceType.VirtualGpu,
        VkPhysicalDeviceType.Cpu => GraphicsDeviceType.Cpu,
        VkPhysicalDeviceType.Other => GraphicsDeviceType.Software,
        _ => GraphicsDeviceType.Unknown
    };
}
