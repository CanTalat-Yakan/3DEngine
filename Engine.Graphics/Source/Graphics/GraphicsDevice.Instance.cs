using System.Runtime.InteropServices;
using System.Text;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice
{
    private static readonly string[] ValidationLayers =
    {
        Utf8(VK_LAYER_KHRONOS_VALIDATION_EXTENSION_NAME)
    };

    private partial void CreateInstance(string appName)
    {
        _validationEnabled = ShouldEnableValidation();

        VkUtf8ReadOnlyString appNameUtf8 = Encoding.UTF8.GetBytes(appName);
        VkUtf8ReadOnlyString engineNameUtf8 = "3DEngine"u8;

        VkApplicationInfo appInfo = new()
        {
            pApplicationName = appNameUtf8,
            applicationVersion = new VkVersion(1, 0, 0),
            pEngineName = engineNameUtf8,
            engineVersion = new VkVersion(1, 0, 0),
            apiVersion = VkVersion.Version_1_2
        };

        var requiredExtensions = _surfaceSource!
            .GetRequiredInstanceExtensions()
            .ToList();
        var debugUtils = Utf8(VK_EXT_DEBUG_UTILS_EXTENSION_NAME);
        if (_validationEnabled && !requiredExtensions.Contains(debugUtils))
            requiredExtensions.Add(debugUtils);

        using var extensions = new VkStringArray(requiredExtensions);
        VkInstanceCreateInfo createInfo = new()
        {
            pApplicationInfo = &appInfo,
            enabledExtensionCount = extensions.Length,
            ppEnabledExtensionNames = extensions
        };

        VkDebugUtilsMessengerCreateInfoEXT debugCreateInfo = default;
        if (_validationEnabled)
        {
            using var validation = new VkStringArray(ValidationLayers);
            createInfo.enabledLayerCount = validation.Length;
            createInfo.ppEnabledLayerNames = validation;
            PopulateDebugMessengerCreateInfo(ref debugCreateInfo);
            createInfo.pNext = &debugCreateInfo;
        }

        vkCreateInstance(&createInfo, null, out _instance).CheckResult();
        _instanceApi = GetApi(_instance);

        if (_validationEnabled)
        {
            _instanceApi.vkCreateDebugUtilsMessengerEXT(_instance, &debugCreateInfo, null, out _debugMessenger).CheckResult();
        }
    }

    private partial void DestroyInstance()
    {
        if (_validationEnabled && _debugMessenger.Handle != 0)
        {
            _instanceApi.vkDestroyDebugUtilsMessengerEXT(_instance, _debugMessenger, null);
        }
        if (_instance.Handle != 0)
        {
            _instanceApi.vkDestroyInstance(_instance);
            _instance = default;
        }
    }

    private static bool ShouldEnableValidation()
    {
#if DEBUG
        return true;
#else
        return Environment.GetEnvironmentVariable("ENGINE_VULKAN_VALIDATION") == "1";
#endif
    }

    private static void PopulateDebugMessengerCreateInfo(ref VkDebugUtilsMessengerCreateInfoEXT createInfo)
    {
        createInfo = new VkDebugUtilsMessengerCreateInfoEXT
        {
            messageSeverity = VkDebugUtilsMessageSeverityFlagsEXT.Verbose |
                              VkDebugUtilsMessageSeverityFlagsEXT.Warning |
                              VkDebugUtilsMessageSeverityFlagsEXT.Error,
            messageType = VkDebugUtilsMessageTypeFlagsEXT.General |
                          VkDebugUtilsMessageTypeFlagsEXT.Validation |
                          VkDebugUtilsMessageTypeFlagsEXT.Performance,
            pfnUserCallback = &DebugCallback
        };
    }

    [UnmanagedCallersOnly]
    private static uint DebugCallback(
        VkDebugUtilsMessageSeverityFlagsEXT severity,
        VkDebugUtilsMessageTypeFlagsEXT type,
        VkDebugUtilsMessengerCallbackDataEXT* data,
        void* userData)
    {
        var message = Marshal.PtrToStringUTF8((nint)data->pMessage) ?? string.Empty;
        Console.WriteLine($"[VK][{severity}][{type}] {message}");
        return 0;
    }
}
