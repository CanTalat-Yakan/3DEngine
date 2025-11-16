using System.Text;
using Vortice.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice : IGraphicsDevice
{
    private const int MaxFramesInFlight = 3;
    private const int MaxPhysicalDeviceNameSize = 256;

    public bool IsInitialized { get; private set; }
    public ISwapchain Swapchain => _swapchainWrapper;
    public GraphicsAdapterInfo AdapterInfo => _adapterInfo;

    public GraphicsDevice() => _swapchainWrapper = new VulkanSwapchain(this);

    public void Initialize(ISurfaceSource surfaceSource, string appName = "3DEngine")
    {
        if (IsInitialized) return;
        _surfaceSource = surfaceSource;
        CreateInstance(appName);
        CreateSurface();
        SelectPhysicalDevice();
        CreateLogicalDevice();
        CreateSwapchainResources();
        CreateSyncObjects();
        CreateDescriptorResources();

        Log.Category("Engine.Graphics").Info($"Graphics device initialized: {_adapterInfo.Name} (Vendor=0x{_adapterInfo.VendorId:X4}, Device=0x{_adapterInfo.DeviceId:X4}, Type={_adapterInfo.DeviceType})");

        IsInitialized = true;
    }

    public void OnResize()
    {
        if (!IsInitialized) return;
        _deviceApi.vkDeviceWaitIdle(_device).CheckResult();
        DestroySwapchainResources();
        CreateSwapchainResources();
        _resizeVersion++;

        Log.Category("Engine.Graphics").Info($"Swapchain resized to {_swapchainExtent.width}x{_swapchainExtent.height}, revision={_resizeVersion}");
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
        _deviceApi.vkDeviceWaitIdle(_device);
        DestroySyncObjects();
        DestroySwapchainResources();
        DestroyDescriptorResources();
        DestroyLogicalDevice();
        DestroySurface();
        DestroyInstance();
        IsInitialized = false;
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

    // Partial contracts implemented across the split files
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
