using System.Text;
using Vortice.Vulkan;
using static Vortice.Vulkan.Vulkan;

namespace Engine;

public sealed unsafe class RendererContext : IDisposable
{
    private const int MaxFramesInFlight = 2;

    public bool IsInitialized { get; private set; }

    public VkInstance Instance;
    public VkPhysicalDevice PhysicalDevice;
    public VkDevice Device;
    public VkSurfaceKHR Surface;
    public VkSwapchainKHR Swapchain;
    public VkFormat SwapchainFormat;
    public VkExtent2D SwapchainExtent;
    public VkImage[] SwapchainImages = Array.Empty<VkImage>();
    public VkImageView[] SwapchainImageViews = Array.Empty<VkImageView>();
    public VkFramebuffer[] Framebuffers = Array.Empty<VkFramebuffer>();
    public VkRenderPass RenderPass;

    public uint GraphicsQueueFamily;
    public uint PresentQueueFamily;
    public VkQueue GraphicsQueue;
    public VkQueue PresentQueue;

    // Add Vortice dispatch tables to use instance/device APIs
    private VkInstanceApi _instanceApi = default;
    private VkDeviceApi _deviceApi = default;

    private VkCommandPool _commandPool;
    private VkCommandBuffer[] _commandBuffers = Array.Empty<VkCommandBuffer>();

    private VkSemaphore[] _imageAvailableSemaphores = Array.Empty<VkSemaphore>();
    private VkSemaphore[] _renderFinishedSemaphores = Array.Empty<VkSemaphore>();
    private VkFence[] _inFlightFences = Array.Empty<VkFence>();

    private int _currentFrame;
    private ISurfaceSource? _surfaceSource;
    private uint _lastAcquiredImageIndex;

    public void Initialize(ISurfaceSource surfaceSource, string appName = "3DEngine")
    {
        if (IsInitialized) return;
        _surfaceSource = surfaceSource;

        // Application/engine info (use Vortice UTF8 wrappers)
        VkUtf8ReadOnlyString pApplicationName = Encoding.UTF8.GetBytes(appName);
        VkUtf8ReadOnlyString pEngineName = "3DEngine"u8;
        VkApplicationInfo appInfo = new()
        {
            pApplicationName = pApplicationName,
            applicationVersion = new VkVersion(1, 0, 0),
            pEngineName = pEngineName,
            engineVersion = new VkVersion(1, 0, 0),
            apiVersion = VkVersion.Version_1_2
        };

        // Instance extensions from surface source
        var requiredExts = surfaceSource.GetRequiredInstanceExtensions();
        List<VkUtf8String> extNames = new(requiredExts.Count);
        foreach (var e in requiredExts)
        {
            extNames.Add(Encoding.UTF8.GetBytes(e));
        }
        using var vkInstanceExtensions = new VkStringArray(extNames);

        VkInstanceCreateInfo instInfo = new()
        {
            pApplicationInfo = &appInfo,
            enabledExtensionCount = vkInstanceExtensions.Length,
            ppEnabledExtensionNames = vkInstanceExtensions
        };

        // Create instance and bind instance API
        vkCreateInstance(&instInfo, out Instance).CheckResult();
        _instanceApi = GetApi(Instance);

        // Surface
        Surface = surfaceSource.CreateSurface(Instance);

        // Physical device selection
        _instanceApi.vkEnumeratePhysicalDevices(Instance, out uint deviceCount).CheckResult();
        if (deviceCount == 0) throw new InvalidOperationException("No Vulkan physical devices.");
        Span<VkPhysicalDevice> phys = stackalloc VkPhysicalDevice[(int)deviceCount];
        _instanceApi.vkEnumeratePhysicalDevices(Instance, phys).CheckResult();

        VkPhysicalDevice? chosen = null;
        foreach (var pd in phys)
        {
            FindQueueFamilies(pd, Surface, out uint gfx, out uint present, out bool suitable);
            if (!suitable) continue;
            _instanceApi.vkGetPhysicalDeviceProperties(pd, out var props);
            if (props.deviceType == VkPhysicalDeviceType.DiscreteGpu)
            { chosen = pd; GraphicsQueueFamily = gfx; PresentQueueFamily = present; break; }
            if (chosen is null)
            { chosen = pd; GraphicsQueueFamily = gfx; PresentQueueFamily = present; }
        }
        if (chosen is null) throw new InvalidOperationException("No suitable GPU found.");
        PhysicalDevice = chosen.Value;

        // Device and queues (use stackalloc like samples)
        float priority = 1.0f;
        var uniqueFamilies = GraphicsQueueFamily == PresentQueueFamily
            ? new uint[] { GraphicsQueueFamily }
            : new uint[] { GraphicsQueueFamily, PresentQueueFamily };
        VkDeviceQueueCreateInfo* queueCreateInfos = stackalloc VkDeviceQueueCreateInfo[2];
        uint qCount = 0;
        foreach (var qf in uniqueFamilies.Distinct())
        {
            queueCreateInfos[qCount++] = new VkDeviceQueueCreateInfo
            {
                queueFamilyIndex = qf,
                queueCount = 1,
                pQueuePriorities = &priority
            };
        }

        List<VkUtf8String> deviceExts = new() { VK_KHR_SWAPCHAIN_EXTENSION_NAME };
        using var vkDeviceExtensions = new VkStringArray(deviceExts);

        VkDeviceCreateInfo devInfo = new()
        {
            queueCreateInfoCount = qCount,
            pQueueCreateInfos = queueCreateInfos,
            enabledExtensionCount = vkDeviceExtensions.Length,
            ppEnabledExtensionNames = vkDeviceExtensions
        };
        _instanceApi.vkCreateDevice(PhysicalDevice, &devInfo, null, out Device).CheckResult();

        // Bind device API
        _deviceApi = GetApi(Instance, Device);
        _deviceApi.vkGetDeviceQueue(Device, GraphicsQueueFamily, 0, out GraphicsQueue);
        _deviceApi.vkGetDeviceQueue(Device, PresentQueueFamily, 0, out PresentQueue);

        CreateSwapchainResources();
        CreateSyncObjects();

        IsInitialized = true;
    }

    private void CreateSwapchainResources()
    {
        var drawable = _surfaceSource!.GetDrawableSize();
        if (drawable.Width == 0 || drawable.Height == 0)
        {
            drawable = (1, 1);
        }

        _instanceApi.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(PhysicalDevice, Surface, out VkSurfaceCapabilitiesKHR caps).CheckResult();

        _instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(PhysicalDevice, Surface, out uint fmtCount).CheckResult();
        Span<VkSurfaceFormatKHR> fmts = stackalloc VkSurfaceFormatKHR[(int)fmtCount];
        _instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(PhysicalDevice, Surface, fmts).CheckResult();

        // Choose a surface format (prefer BGRA8 if available)
        VkSurfaceFormatKHR chosenFmt = fmts.Length > 0 ? fmts[0] : new VkSurfaceFormatKHR(VkFormat.B8G8R8A8Unorm, 0);
        for (int i = 0; i < fmts.Length; i++)
        {
            if (fmts[i].format == VkFormat.B8G8R8A8Unorm)
            {
                chosenFmt = fmts[i];
                break;
            }
        }
        SwapchainFormat = chosenFmt.format;

        _instanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(PhysicalDevice, Surface, out uint pmCount).CheckResult();
        Span<VkPresentModeKHR> modes = stackalloc VkPresentModeKHR[(int)pmCount];
        _instanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(PhysicalDevice, Surface, modes).CheckResult();
        VkPresentModeKHR presentMode = VkPresentModeKHR.Fifo;
        for (int i = 0; i < modes.Length; i++)
        {
            if (modes[i] == VkPresentModeKHR.Mailbox) { presentMode = VkPresentModeKHR.Mailbox; break; }
            if (modes[i] == VkPresentModeKHR.Immediate) { presentMode = VkPresentModeKHR.Immediate; }
        }

        VkExtent2D extent;
        if (caps.currentExtent.width != uint.MaxValue)
        {
            extent = caps.currentExtent;
        }
        else
        {
            extent = new VkExtent2D
            {
                width = Math.Clamp(drawable.Width, caps.minImageExtent.width, caps.maxImageExtent.width),
                height = Math.Clamp(drawable.Height, caps.minImageExtent.height, caps.maxImageExtent.height)
            };
        }
        SwapchainExtent = extent;

        uint imageCount = caps.minImageCount + 1;
        if (caps.maxImageCount > 0 && imageCount > caps.maxImageCount)
            imageCount = caps.maxImageCount;

        VkSwapchainCreateInfoKHR ci = new()
        {
            surface = Surface,
            minImageCount = imageCount,
            imageFormat = SwapchainFormat,
            imageColorSpace = chosenFmt.colorSpace,
            imageExtent = extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            preTransform = caps.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = presentMode,
            clipped = true,
            oldSwapchain = Swapchain
        };
        var queueFamilyIndices = GraphicsQueueFamily == PresentQueueFamily ? Array.Empty<uint>() : new[] { GraphicsQueueFamily, PresentQueueFamily };
        if (queueFamilyIndices.Length > 0)
        {
            fixed (uint* q = queueFamilyIndices)
            {
                ci.imageSharingMode = VkSharingMode.Concurrent;
                ci.queueFamilyIndexCount = (uint)queueFamilyIndices.Length;
                ci.pQueueFamilyIndices = q;
                _deviceApi.vkCreateSwapchainKHR(Device, &ci, null, out Swapchain).CheckResult();
            }
        }
        else
        {
            ci.imageSharingMode = VkSharingMode.Exclusive;
            _deviceApi.vkCreateSwapchainKHR(Device, &ci, null, out Swapchain).CheckResult();
        }

        _deviceApi.vkGetSwapchainImagesKHR(Device, Swapchain, out uint count).CheckResult();
        Span<VkImage> images = stackalloc VkImage[(int)count];
        _deviceApi.vkGetSwapchainImagesKHR(Device, Swapchain, images).CheckResult();
        SwapchainImages = images.ToArray();

        SwapchainImageViews = new VkImageView[count];
        for (int i = 0; i < count; i++)
        {
            VkImageViewCreateInfo viewInfo = new()
            {
                image = SwapchainImages[i],
                viewType = VkImageViewType.Image2D,
                format = SwapchainFormat,
                components = VkComponentMapping.Rgba,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
            };
            _deviceApi.vkCreateImageView(Device, &viewInfo, null, out SwapchainImageViews[i]).CheckResult();
        }

        var colorAttachment = new VkAttachmentDescription
        {
            format = SwapchainFormat,
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.PresentSrcKHR
        };
        var colorAttachmentRef = new VkAttachmentReference { attachment = 0, layout = VkImageLayout.ColorAttachmentOptimal };
        var subpass = new VkSubpassDescription
        {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentRef
        };
        var dep = new VkSubpassDependency
        {
            srcSubpass = VK_SUBPASS_EXTERNAL,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput,
            srcAccessMask = 0,
            dstAccessMask = VkAccessFlags.ColorAttachmentWrite
        };
        VkRenderPassCreateInfo rpInfo = new()
        {
            attachmentCount = 1,
            pAttachments = &colorAttachment,
            subpassCount = 1,
            pSubpasses = &subpass,
            dependencyCount = 1,
            pDependencies = &dep
        };
        _deviceApi.vkCreateRenderPass(Device, &rpInfo, null, out RenderPass).CheckResult();

        Framebuffers = new VkFramebuffer[count];
        for (int i = 0; i < count; i++)
        {
            fixed (VkImageView* pAttachment = &SwapchainImageViews[i])
            {
                VkFramebufferCreateInfo fbInfo = new()
                {
                    renderPass = RenderPass,
                    attachmentCount = 1,
                    pAttachments = pAttachment,
                    width = extent.width,
                    height = extent.height,
                    layers = 1
                };
                _deviceApi.vkCreateFramebuffer(Device, &fbInfo, null, out Framebuffers[i]).CheckResult();
            }
        }

        VkCommandPoolCreateInfo poolInfo = new()
        {
            flags = VkCommandPoolCreateFlags.ResetCommandBuffer,
            queueFamilyIndex = GraphicsQueueFamily
        };
        _deviceApi.vkCreateCommandPool(Device, &poolInfo, null, out _commandPool).CheckResult();

        _commandBuffers = new VkCommandBuffer[MaxFramesInFlight];
        VkCommandBufferAllocateInfo allocInfo = new()
        {
            commandPool = _commandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = (uint)_commandBuffers.Length
        };
        fixed (VkCommandBuffer* pcb = _commandBuffers)
            _deviceApi.vkAllocateCommandBuffers(Device, &allocInfo, pcb).CheckResult();
    }

    private void DestroySwapchainResources()
    {
        foreach (var fb in Framebuffers)
            if (fb.Handle != 0) _deviceApi.vkDestroyFramebuffer(Device, fb);
        if (RenderPass.Handle != 0) _deviceApi.vkDestroyRenderPass(Device, RenderPass);
        foreach (var iv in SwapchainImageViews)
            if (iv.Handle != 0) _deviceApi.vkDestroyImageView(Device, iv);
        if (Swapchain.Handle != 0) _deviceApi.vkDestroySwapchainKHR(Device, Swapchain);
        if (_commandPool.Handle != 0) _deviceApi.vkDestroyCommandPool(Device, _commandPool);

        Framebuffers = Array.Empty<VkFramebuffer>();
        SwapchainImageViews = Array.Empty<VkImageView>();
        SwapchainImages = Array.Empty<VkImage>();
        _commandBuffers = Array.Empty<VkCommandBuffer>();
        RenderPass = default;
        Swapchain = default;
        _commandPool = default;
    }

    private void CreateSyncObjects()
    {
        _imageAvailableSemaphores = new VkSemaphore[MaxFramesInFlight];
        _renderFinishedSemaphores = new VkSemaphore[MaxFramesInFlight];
        _inFlightFences = new VkFence[MaxFramesInFlight];

        VkFenceCreateInfo fenceInfo = new() { flags = VkFenceCreateFlags.Signaled };
        for (int i = 0; i < MaxFramesInFlight; i++)
        {
            _deviceApi.vkCreateSemaphore(Device, out _imageAvailableSemaphores[i]).CheckResult();
            _deviceApi.vkCreateSemaphore(Device, out _renderFinishedSemaphores[i]).CheckResult();
            _deviceApi.vkCreateFence(Device, &fenceInfo, null, out _inFlightFences[i]).CheckResult();
        }
    }

    public void OnResize()
    {
        if (!IsInitialized) return;
        _deviceApi.vkDeviceWaitIdle(Device).CheckResult();
        DestroySwapchainResources();
        CreateSwapchainResources();
    }

    public CommandRecordingContext BeginFrame(RenderWorld world, out uint imageIndex)
    {
        if (!IsInitialized) throw new InvalidOperationException("Vulkan not initialized");

        _deviceApi.vkWaitForFences(Device, _inFlightFences[_currentFrame], true, ulong.MaxValue).CheckResult();

        var acquireRes = _deviceApi.vkAcquireNextImageKHR(Device, Swapchain, ulong.MaxValue, _imageAvailableSemaphores[_currentFrame], default, out imageIndex);
        if (acquireRes == VkResult.ErrorOutOfDateKHR)
        {
            OnResize();
            return BeginFrame(world, out imageIndex);
        }
        acquireRes.CheckResult();
        _lastAcquiredImageIndex = imageIndex;

        var cmd = _commandBuffers[_currentFrame];
        VkCommandBufferBeginInfo beginInfo = new();
        _deviceApi.vkResetCommandBuffer(cmd, 0).CheckResult();
        _deviceApi.vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();

        var clear = world.TryGet<RenderClearColor>() is { } cc
            ? new VkClearValue(new VkClearColorValue(cc.R, cc.G, cc.B, cc.A))
            : new VkClearValue(new VkClearColorValue(0f, 0f, 0f, 1f));
        VkRenderPassBeginInfo rpBegin = new()
        {
            renderPass = RenderPass,
            framebuffer = Framebuffers[imageIndex],
            renderArea = new VkRect2D(new VkOffset2D(0, 0), SwapchainExtent),
            clearValueCount = 1,
            pClearValues = &clear
        };
        _deviceApi.vkCmdBeginRenderPass(cmd, &rpBegin, VkSubpassContents.Inline);

        return new CommandRecordingContext
        {
            CommandBuffer = cmd,
            SwapchainExtent = SwapchainExtent,
            RenderPass = RenderPass,
            Framebuffer = Framebuffers[imageIndex]
        };
    }

    public CommandRecordingContext BeginFrame(RenderWorld world) => BeginFrame(world, out _lastAcquiredImageIndex);

    public void EndFrame(CommandRecordingContext ctx, uint imageIndex)
    {
        _deviceApi.vkCmdEndRenderPass(ctx.CommandBuffer);
        _deviceApi.vkEndCommandBuffer(ctx.CommandBuffer).CheckResult();

        var waitStages = VkPipelineStageFlags.ColorAttachmentOutput;
        VkSubmitInfo submitInfo = new()
        {
            waitSemaphoreCount = 1,
            commandBufferCount = 1,
            signalSemaphoreCount = 1,
        };
        VkSemaphore* pWait = stackalloc VkSemaphore[1];
        pWait[0] = _imageAvailableSemaphores[_currentFrame];
        VkSemaphore* pSignal = stackalloc VkSemaphore[1];
        pSignal[0] = _renderFinishedSemaphores[_currentFrame];
        VkCommandBuffer* pCmd = stackalloc VkCommandBuffer[1];
        pCmd[0] = ctx.CommandBuffer;
        submitInfo.pWaitSemaphores = pWait;
        submitInfo.pWaitDstStageMask = &waitStages;
        submitInfo.pCommandBuffers = pCmd;
        submitInfo.pSignalSemaphores = pSignal;

        _deviceApi.vkResetFences(Device, _inFlightFences[_currentFrame]).CheckResult();
        _deviceApi.vkQueueSubmit(GraphicsQueue, 1, &submitInfo, _inFlightFences[_currentFrame]).CheckResult();

        VkPresentInfoKHR presentInfo = new()
        {
            waitSemaphoreCount = 1,
            swapchainCount = 1,
        };
        VkSemaphore* pWaitPresent = stackalloc VkSemaphore[1];
        pWaitPresent[0] = _renderFinishedSemaphores[_currentFrame];
        VkSwapchainKHR* pSwap = stackalloc VkSwapchainKHR[1];
        pSwap[0] = Swapchain;
        uint* pIndex = stackalloc uint[1];
        pIndex[0] = imageIndex;
        presentInfo.pWaitSemaphores = pWaitPresent;
        presentInfo.pSwapchains = pSwap;
        presentInfo.pImageIndices = pIndex;

        var presentRes = _deviceApi.vkQueuePresentKHR(PresentQueue, &presentInfo);
        if (presentRes == VkResult.ErrorOutOfDateKHR || presentRes == VkResult.SuboptimalKHR)
        {
            OnResize();
        }
        else
        {
            presentRes.CheckResult();
        }

        _currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
    }

    public void EndFrame(CommandRecordingContext ctx) => EndFrame(ctx, _lastAcquiredImageIndex);

    public void Dispose()
    {
        if (!IsInitialized) return;
        _deviceApi.vkDeviceWaitIdle(Device);

        foreach (var f in _inFlightFences) if (f.Handle != 0) _deviceApi.vkDestroyFence(Device, f);
        foreach (var s in _imageAvailableSemaphores) if (s.Handle != 0) _deviceApi.vkDestroySemaphore(Device, s);
        foreach (var s in _renderFinishedSemaphores) if (s.Handle != 0) _deviceApi.vkDestroySemaphore(Device, s);

        DestroySwapchainResources();

        if (Device.Handle != 0) _deviceApi.vkDestroyDevice(Device);
        if (Surface.Handle != 0) _instanceApi.vkDestroySurfaceKHR(Instance, Surface);
        if (Instance.Handle != 0) _instanceApi.vkDestroyInstance(Instance);

        IsInitialized = false;
    }

    private void FindQueueFamilies(VkPhysicalDevice device, VkSurfaceKHR surface, out uint graphics, out uint present, out bool suitable)
    {
        graphics = present = uint.MaxValue;
        suitable = false;
        _instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(device, out uint count);
        Span<VkQueueFamilyProperties> props = stackalloc VkQueueFamilyProperties[(int)count];
        _instanceApi.vkGetPhysicalDeviceQueueFamilyProperties(device, props);
        for (uint i = 0; i < count; i++)
        {
            if ((props[(int)i].queueFlags & VkQueueFlags.Graphics) != 0)
                graphics = i;
            _instanceApi.vkGetPhysicalDeviceSurfaceSupportKHR(device, i, surface, out VkBool32 supports);
            if (supports) present = i;
            if (graphics != uint.MaxValue && present != uint.MaxValue) { suitable = true; break; }
        }
    }
}