using System.Diagnostics;
using System.Text;
using Vortice.Vulkan;

namespace Engine;

/// <summary>Vulkan-backed graphics device implementing resource creation and frame management.</summary>
public sealed unsafe partial class GraphicsDevice : IGraphicsDevice
{
    private const int MaxFramesInFlight = 3;
    private const int MaxPhysicalDeviceNameSize = 256;
    private static readonly ILogger Logger = Log.Category("Engine.Graphics");

    public bool IsInitialized { get; private set; }
    public ISwapchain Swapchain => _swapchainWrapper;
    public GraphicsAdapterInfo AdapterInfo => _adapterInfo;
    public int FramesInFlight => MaxFramesInFlight;

    public GraphicsDevice() => _swapchainWrapper = new VulkanSwapchain(this);

    public void Initialize(ISurfaceSource surfaceSource, string appName = "3DEngine")
    {
        if (IsInitialized) return;
        _surfaceSource = surfaceSource;

        Logger.Info("Initializing Vulkan graphics device...");
        var totalSw = Stopwatch.StartNew();

        Logger.Info("Step 1/6: Creating Vulkan instance — loading Vulkan library, setting up validation layers, and creating the VkInstance...");
        var sw = Stopwatch.StartNew();
        CreateInstance(appName);
        Logger.Info($"Step 1/6: Vulkan instance created in {sw.ElapsedMilliseconds}ms");

        Logger.Info("Step 2/6: Creating window surface — binding the platform window to Vulkan via VkSurfaceKHR...");
        sw.Restart();
        CreateSurface();
        Logger.Info($"Step 2/6: Window surface created in {sw.ElapsedMilliseconds}ms");

        Logger.Info("Step 3/6: Selecting physical device — enumerating GPUs, scoring capabilities, and choosing the best adapter...");
        sw.Restart();
        SelectPhysicalDevice();
        Logger.Info($"Step 3/6: Physical device selected in {sw.ElapsedMilliseconds}ms — {_adapterInfo.Name} (Vendor=0x{_adapterInfo.VendorId:X4}, Device=0x{_adapterInfo.DeviceId:X4}, Type={_adapterInfo.DeviceType})");

        Logger.Info("Step 4/6: Creating logical device — setting up device queues (graphics + present) and enabling required extensions...");
        sw.Restart();
        CreateLogicalDevice();
        Logger.Info($"Step 4/6: Logical device created in {sw.ElapsedMilliseconds}ms (graphicsQueue={_graphicsQueueFamily}, presentQueue={_presentQueueFamily})");

        Logger.Info("Step 5/6: Creating swapchain resources — swapchain, image views, depth buffer, render pass, framebuffers, and command pool...");
        sw.Restart();
        CreateSwapchainResources();
        Logger.Info($"Step 5/6: Swapchain resources created in {sw.ElapsedMilliseconds}ms ({_swapchainImages.Length} images, {_swapchainExtent.width}x{_swapchainExtent.height}, format={_swapchainFormat})");

        Logger.Info("Step 6/6: Creating synchronization objects — semaphores and fences for frame-in-flight management...");
        sw.Restart();
        CreateSyncObjects();
        Logger.Info($"Step 6/6: Sync objects created in {sw.ElapsedMilliseconds}ms ({MaxFramesInFlight} frames-in-flight)");

        Logger.Info("Creating descriptor resources — descriptor set layouts and descriptor pool for uniform buffers and samplers...");
        sw.Restart();
        CreateDescriptorResources();
        Logger.Info($"Descriptor resources created in {sw.ElapsedMilliseconds}ms");

        totalSw.Stop();
        Logger.Info($"Graphics device initialized successfully in {totalSw.ElapsedMilliseconds}ms — {_adapterInfo.Name} (Vendor=0x{_adapterInfo.VendorId:X4}, Device=0x{_adapterInfo.DeviceId:X4}, Type={_adapterInfo.DeviceType})");

        IsInitialized = true;
    }

    public void OnResize()
    {
        if (!IsInitialized) return;
        Logger.Info("Swapchain resize requested — waiting for device idle before recreating...");
        _deviceApi.vkDeviceWaitIdle(_device).CheckResult();
        Logger.Debug("Device idle — destroying old swapchain resources...");
        DestroySwapchainResources();
        Logger.Debug("Old swapchain resources destroyed — creating new swapchain resources...");
        CreateSwapchainResources();
        _resizeVersion++;
        Logger.Info($"Swapchain resized to {_swapchainExtent.width}x{_swapchainExtent.height} (revision={_resizeVersion}, images={_swapchainImages.Length})");
    }

    public IFrameContext BeginFrame(ClearColor? clearOverride = null) => BeginFrameInternal(clearOverride ?? ClearColor.Black);

    public void EndFrame(IFrameContext frameContext)
    {
        SubmitFrame((VulkanFrameContext)frameContext);
        frameContext.Dispose();
    }

    public void Dispose()
    {
        if (!IsInitialized) return;
        Logger.Info("Disposing graphics device — waiting for device idle...");
        _deviceApi.vkDeviceWaitIdle(_device);
        Logger.Debug("Destroying sync objects (semaphores, fences)...");
        DestroySyncObjects();
        Logger.Debug("Destroying swapchain resources (framebuffers, image views, render pass, command pool)...");
        DestroySwapchainResources();
        Logger.Debug("Destroying descriptor resources (pool, layouts)...");
        DestroyDescriptorResources();
        Logger.Debug("Destroying logical device...");
        DestroyLogicalDevice();
        Logger.Debug("Destroying window surface...");
        DestroySurface();
        Logger.Debug("Destroying Vulkan instance and debug messenger...");
        DestroyInstance();
        IsInitialized = false;
        Logger.Info("Graphics device disposed successfully.");
    }

    // Shared state
    private ISurfaceSource? _surfaceSource;
    private VkInstance _instance;
    private VkPhysicalDevice _physicalDevice;
    private VkDevice _device;
    private VkSurfaceKHR _surface;
    private VkSwapchainKHR _swapchain;
    private VkFormat _swapchainFormat;
    private VkExtent2D _swapchainExtent;
    private VkImage[] _swapchainImages = Array.Empty<VkImage>();
    private VkImageView[] _swapchainImageViews = Array.Empty<VkImageView>();
    private VkFramebuffer[] _framebuffers = Array.Empty<VkFramebuffer>();
    private VkRenderPass _renderPass;
    private uint _graphicsQueueFamily;
    private uint _presentQueueFamily;
    private VkQueue _graphicsQueue;
    private VkQueue _presentQueue;
    private VkInstanceApi _instanceApi = null!;
    private VkDeviceApi _deviceApi = null!;
    private VkCommandPool _commandPool;
    private VkCommandBuffer[] _commandBuffers = Array.Empty<VkCommandBuffer>();
    private VkSemaphore[] _imageAvailableSemaphores = Array.Empty<VkSemaphore>();
    private VkSemaphore[] _renderFinishedSemaphores = Array.Empty<VkSemaphore>();
    private VkFence[] _inFlightFences = Array.Empty<VkFence>();
    private int _currentFrame;
    private uint _lastAcquiredImageIndex;
    private readonly VulkanSwapchain _swapchainWrapper;
    private ulong _resizeVersion;
    private VkDebugUtilsMessengerEXT _debugMessenger;
    private bool _validationEnabled;
    private GraphicsAdapterInfo _adapterInfo = GraphicsAdapterInfo.Unknown;
    private VkImage _depthImage;
    private VkDeviceMemory _depthImageMemory;
    private VkImageView _depthImageView;

    private static string Utf8(ReadOnlySpan<byte> value)
    {
        if (value.IsEmpty) return string.Empty;
        if (value[^1] == 0) value = value[..^1];
        return Encoding.UTF8.GetString(value);
    }

    // Partial methods implemented across split files
    private partial void CreateInstance(string appName);
    private partial void DestroyInstance();
    private partial void CreateSurface();
    private partial void DestroySurface();
    private partial void SelectPhysicalDevice();
    private partial void CreateLogicalDevice();
    private partial void DestroyLogicalDevice();
    private partial void CreateSwapchainResources();
    private partial void DestroySwapchainResources();
    private partial void CreateSyncObjects();
    private partial void DestroySyncObjects();
    private partial IFrameContext BeginFrameInternal(ClearColor clearColor);
    private partial void SubmitFrame(VulkanFrameContext ctx);
}
