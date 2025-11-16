using System.Linq;
using Vortice.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice
{
    private partial void CreateSwapchainResources()
    {
        var drawable = _surfaceSource!.GetDrawableSize();
        if (drawable.Width == 0 || drawable.Height == 0)
            drawable = (1, 1);

        var support = QuerySwapchainSupport(_physicalDevice);
        var surfaceFormat = ChooseSwapchainFormat(support.Formats);
        var presentMode = ChoosePresentMode(support.PresentModes);
        var extent = ChooseSwapExtent(support.Capabilities, (uint)drawable.Width, (uint)drawable.Height);

        _swapchainFormat = surfaceFormat.format;
        _swapchainExtent = extent;

        uint imageCount = support.Capabilities.minImageCount + 1;
        if (support.Capabilities.maxImageCount > 0 && imageCount > support.Capabilities.maxImageCount)
            imageCount = support.Capabilities.maxImageCount;

        VkSwapchainCreateInfoKHR createInfo = new()
        {
            surface = _surface,
            minImageCount = imageCount,
            imageFormat = surfaceFormat.format,
            imageColorSpace = surfaceFormat.colorSpace,
            imageExtent = extent,
            imageArrayLayers = 1,
            imageUsage = VkImageUsageFlags.ColorAttachment,
            preTransform = support.Capabilities.currentTransform,
            compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque,
            presentMode = presentMode,
            clipped = true,
            oldSwapchain = _swapchain
        };

        if (_graphicsQueueFamily != _presentQueueFamily)
        {
            var families = stackalloc uint[2];
            families[0] = _graphicsQueueFamily;
            families[1] = _presentQueueFamily;
            createInfo.imageSharingMode = VkSharingMode.Concurrent;
            createInfo.queueFamilyIndexCount = 2;
            createInfo.pQueueFamilyIndices = families;
        }
        else
        {
            createInfo.imageSharingMode = VkSharingMode.Exclusive;
        }

        _deviceApi.vkCreateSwapchainKHR(_device, &createInfo, null, out _swapchain).CheckResult();

        _deviceApi.vkGetSwapchainImagesKHR(_device, _swapchain, out uint count).CheckResult();
        Span<VkImage> images = stackalloc VkImage[(int)count];
        _deviceApi.vkGetSwapchainImagesKHR(_device, _swapchain, images).CheckResult();
        _swapchainImages = images.ToArray();

        CreateImageViews();
        CreateDepthResources();
        CreateRenderPass();
        CreateFramebuffers();
        CreateCommandPoolAndBuffers();
    }

    private partial void DestroySwapchainResources()
    {
        foreach (var fb in _framebuffers)
            if (fb.Handle != 0) _deviceApi.vkDestroyFramebuffer(_device, fb);
        foreach (var iv in _swapchainImageViews)
            if (iv.Handle != 0) _deviceApi.vkDestroyImageView(_device, iv);
        if (_depthImageView.Handle != 0)
            _deviceApi.vkDestroyImageView(_device, _depthImageView);
        if (_depthImage.Handle != 0)
            _deviceApi.vkDestroyImage(_device, _depthImage);
        if (_depthImageMemory.Handle != 0)
            _deviceApi.vkFreeMemory(_device, _depthImageMemory);
        if (_renderPass.Handle != 0)
            _deviceApi.vkDestroyRenderPass(_device, _renderPass);
        if (_swapchain.Handle != 0)
            _deviceApi.vkDestroySwapchainKHR(_device, _swapchain);
        if (_commandPool.Handle != 0)
            _deviceApi.vkDestroyCommandPool(_device, _commandPool);

        _framebuffers = Array.Empty<VkFramebuffer>();
        _swapchainImageViews = Array.Empty<VkImageView>();
        _swapchainImages = Array.Empty<VkImage>();
        _commandBuffers = Array.Empty<VkCommandBuffer>();
        _renderPass = default;
        _swapchain = default;
        _commandPool = default;
        _depthImage = default;
        _depthImageMemory = default;
        _depthImageView = default;
    }

    private (VkSurfaceCapabilitiesKHR Capabilities, VkSurfaceFormatKHR[] Formats, VkPresentModeKHR[] PresentModes) QuerySwapchainSupport(VkPhysicalDevice device)
    {
        _instanceApi.vkGetPhysicalDeviceSurfaceCapabilitiesKHR(device, _surface, out VkSurfaceCapabilitiesKHR capabilities).CheckResult();
        _instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(device, _surface, out uint formatCount).CheckResult();
        Span<VkSurfaceFormatKHR> formats = stackalloc VkSurfaceFormatKHR[(int)formatCount];
        _instanceApi.vkGetPhysicalDeviceSurfaceFormatsKHR(device, _surface, formats).CheckResult();
        _instanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(device, _surface, out uint modeCount).CheckResult();
        Span<VkPresentModeKHR> modes = stackalloc VkPresentModeKHR[(int)modeCount];
        _instanceApi.vkGetPhysicalDeviceSurfacePresentModesKHR(device, _surface, modes).CheckResult();
        return (capabilities, formats.ToArray(), modes.ToArray());
    }

    private static VkSurfaceFormatKHR ChooseSwapchainFormat(VkSurfaceFormatKHR[] formats)
    {
        if (formats.Length == 1 && formats[0].format == VkFormat.Undefined)
            return formats[0] with { format = VkFormat.B8G8R8A8Unorm };

        foreach (var format in formats)
        {
            if (format.format == VkFormat.B8G8R8A8Unorm)
                return format;
        }

        return formats[0];
    }

    private static VkPresentModeKHR ChoosePresentMode(VkPresentModeKHR[] modes)
    {
        if (modes.Contains(VkPresentModeKHR.Mailbox))
            return VkPresentModeKHR.Mailbox;
        if (modes.Contains(VkPresentModeKHR.Immediate))
            return VkPresentModeKHR.Immediate;
        return VkPresentModeKHR.Fifo;
    }

    private static VkExtent2D ChooseSwapExtent(VkSurfaceCapabilitiesKHR caps, uint width, uint height)
    {
        if (caps.currentExtent.width != uint.MaxValue)
            return caps.currentExtent;

        return new VkExtent2D
        {
            width = Math.Clamp(width, caps.minImageExtent.width, caps.maxImageExtent.width),
            height = Math.Clamp(height, caps.minImageExtent.height, caps.maxImageExtent.height)
        };
    }

    private void CreateDepthResources()
    {
        // For now always use a 32-bit float depth buffer.
        VkFormat depthFormat = VkFormat.D32Sfloat;

        VkImageCreateInfo imageInfo = new()
        {
            imageType = VkImageType.Image2D,
            format = depthFormat,
            extent = new VkExtent3D(_swapchainExtent.width, _swapchainExtent.height, 1),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = VkImageUsageFlags.DepthStencilAttachment,
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined
        };

        _deviceApi.vkCreateImage(_device, &imageInfo, null, out _depthImage).CheckResult();
        _deviceApi.vkGetImageMemoryRequirements(_device, _depthImage, out VkMemoryRequirements req);

        VkMemoryAllocateInfo allocInfo = new()
        {
            allocationSize = req.size,
            memoryTypeIndex = FindMemoryType(req.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
        };

        _deviceApi.vkAllocateMemory(_device, &allocInfo, null, out _depthImageMemory).CheckResult();
        _deviceApi.vkBindImageMemory(_device, _depthImage, _depthImageMemory, 0).CheckResult();

        VkImageViewCreateInfo viewInfo = new()
        {
            image = _depthImage,
            viewType = VkImageViewType.Image2D,
            format = depthFormat,
            subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Depth, 0, 1, 0, 1)
        };

        _deviceApi.vkCreateImageView(_device, &viewInfo, null, out _depthImageView).CheckResult();
    }

    private void CreateImageViews()
    {
        _swapchainImageViews = new VkImageView[_swapchainImages.Length];
        for (int i = 0; i < _swapchainImages.Length; i++)
        {
            VkImageViewCreateInfo viewInfo = new()
            {
                image = _swapchainImages[i],
                viewType = VkImageViewType.Image2D,
                format = _swapchainFormat,
                components = VkComponentMapping.Rgba,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
            };
            _deviceApi.vkCreateImageView(_device, &viewInfo, null, out _swapchainImageViews[i]).CheckResult();
        }
    }

    private void CreateRenderPass()
    {
        var colorAttachment = new VkAttachmentDescription
        {
            format = _swapchainFormat,
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.Store,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.PresentSrcKHR
        };

        var depthAttachment = new VkAttachmentDescription
        {
            format = VkFormat.D32Sfloat,
            samples = VkSampleCountFlags.Count1,
            loadOp = VkAttachmentLoadOp.Clear,
            storeOp = VkAttachmentStoreOp.DontCare,
            stencilLoadOp = VkAttachmentLoadOp.DontCare,
            stencilStoreOp = VkAttachmentStoreOp.DontCare,
            initialLayout = VkImageLayout.Undefined,
            finalLayout = VkImageLayout.DepthStencilAttachmentOptimal
        };

        VkAttachmentDescription* attachments = stackalloc VkAttachmentDescription[2];
        attachments[0] = colorAttachment;
        attachments[1] = depthAttachment;

        var colorAttachmentRef = new VkAttachmentReference { attachment = 0, layout = VkImageLayout.ColorAttachmentOptimal };
        var depthAttachmentRef = new VkAttachmentReference { attachment = 1, layout = VkImageLayout.DepthStencilAttachmentOptimal };

        var subpass = new VkSubpassDescription
        {
            pipelineBindPoint = VkPipelineBindPoint.Graphics,
            colorAttachmentCount = 1,
            pColorAttachments = &colorAttachmentRef,
            pDepthStencilAttachment = &depthAttachmentRef
        };

        var dependency = new VkSubpassDependency
        {
            srcSubpass = uint.MaxValue,
            dstSubpass = 0,
            srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests,
            dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput | VkPipelineStageFlags.EarlyFragmentTests,
            srcAccessMask = 0,
            dstAccessMask = VkAccessFlags.ColorAttachmentWrite | VkAccessFlags.DepthStencilAttachmentWrite
        };

        VkRenderPassCreateInfo renderPassInfo = new()
        {
            attachmentCount = 2,
            pAttachments = attachments,
            subpassCount = 1,
            pSubpasses = &subpass,
            dependencyCount = 1,
            pDependencies = &dependency
        };

        _deviceApi.vkCreateRenderPass(_device, &renderPassInfo, null, out _renderPass).CheckResult();
    }

    private void CreateFramebuffers()
    {
        _framebuffers = new VkFramebuffer[_swapchainImageViews.Length];
        for (int i = 0; i < _swapchainImageViews.Length; i++)
        {
            VkImageView* attachments = stackalloc VkImageView[2];
            attachments[0] = _swapchainImageViews[i];
            attachments[1] = _depthImageView;

            VkFramebufferCreateInfo framebufferInfo = new()
            {
                renderPass = _renderPass,
                attachmentCount = 2,
                pAttachments = attachments,
                width = _swapchainExtent.width,
                height = _swapchainExtent.height,
                layers = 1
            };

            _deviceApi.vkCreateFramebuffer(_device, &framebufferInfo, null, out _framebuffers[i]).CheckResult();
        }
    }

    private void CreateCommandPoolAndBuffers()
    {
        VkCommandPoolCreateInfo poolInfo = new()
        {
            flags = VkCommandPoolCreateFlags.ResetCommandBuffer,
            queueFamilyIndex = _graphicsQueueFamily
        };

        _deviceApi.vkCreateCommandPool(_device, &poolInfo, null, out _commandPool).CheckResult();

        _commandBuffers = new VkCommandBuffer[MaxFramesInFlight];
        VkCommandBufferAllocateInfo allocInfo = new()
        {
            commandPool = _commandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = (uint)_commandBuffers.Length
        };

        fixed (VkCommandBuffer* buffers = _commandBuffers)
        {
            _deviceApi.vkAllocateCommandBuffers(_device, &allocInfo, buffers).CheckResult();
        }
    }
}
