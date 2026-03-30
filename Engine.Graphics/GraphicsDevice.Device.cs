using System.Linq;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice
{
    private static readonly string[] DeviceExtensions =
    {
        Utf8(VK_KHR_SWAPCHAIN_EXTENSION_NAME)
    };

    private partial void CreateLogicalDevice()
    {
        float priority = 1.0f;
        var families = UniqueQueueFamilies().ToArray();
        VkDeviceQueueCreateInfo* queueInfos = stackalloc VkDeviceQueueCreateInfo[families.Length];
        for (int i = 0; i < families.Length; i++)
        {
            queueInfos[i] = new VkDeviceQueueCreateInfo
            {
                queueFamilyIndex = families[i],
                queueCount = 1,
                pQueuePriorities = &priority
            };
        }

        using var deviceExts = new VkStringArray(DeviceExtensions);

        VkPhysicalDeviceFeatures features = new()
        {
            samplerAnisotropy = true
        };

        VkDeviceCreateInfo createInfo = new()
        {
            queueCreateInfoCount = (uint)families.Length,
            pQueueCreateInfos = queueInfos,
            enabledExtensionCount = deviceExts.Length,
            ppEnabledExtensionNames = deviceExts,
            pEnabledFeatures = &features
        };

        if (_validationEnabled)
        {
            using var validation = new VkStringArray(ValidationLayers);
            createInfo.enabledLayerCount = validation.Length;
            createInfo.ppEnabledLayerNames = validation;
        }

        _instanceApi.vkCreateDevice(_physicalDevice, &createInfo, null, out _device).CheckResult();
        _deviceApi = GetApi(_instance, _device);
        _deviceApi.vkGetDeviceQueue(_device, _graphicsQueueFamily, 0, out _graphicsQueue);
        _deviceApi.vkGetDeviceQueue(_device, _presentQueueFamily, 0, out _presentQueue);
    }

    private partial void DestroyLogicalDevice()
    {
        if (_device.Handle != 0)
        {
            _deviceApi.vkDestroyDevice(_device);
            _device = default;
        }
    }

    private IEnumerable<uint> UniqueQueueFamilies()
    {
        if (_graphicsQueueFamily == _presentQueueFamily)
        {
            yield return _graphicsQueueFamily;
        }
        else
        {
            yield return _graphicsQueueFamily;
            yield return _presentQueueFamily;
        }
    }
}
