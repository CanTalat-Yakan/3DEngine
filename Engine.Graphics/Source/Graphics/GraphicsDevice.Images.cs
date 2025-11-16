using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Engine;

// Image, image view, and sampler abstractions implemented for Vulkan.
public sealed unsafe partial class GraphicsDevice
{
    private sealed class VulkanImage : IImage
    {
        private readonly GraphicsDevice _device;
        internal VkImage Image;
        internal VkDeviceMemory Memory;
        internal VkImageLayout Layout;
        public ImageDesc Description { get; }

        public VulkanImage(GraphicsDevice device, VkImage image, VkDeviceMemory memory, ImageDesc desc)
        {
            _device = device;
            Image = image;
            Memory = memory;
            Description = desc;
            Layout = VkImageLayout.Undefined;
        }

        public void Dispose()
        {
            if (Image.Handle != 0)
            {
                _device._deviceApi.vkDestroyImage(_device._device, Image);
                Image = default;
            }
            if (Memory.Handle != 0)
            {
                _device._deviceApi.vkFreeMemory(_device._device, Memory);
                Memory = default;
            }
        }
    }

    private sealed class VulkanImageView : IImageView
    {
        private readonly GraphicsDevice _device;
        public IImage Image { get; }
        internal VkImageView View;

        public VulkanImageView(GraphicsDevice device, IImage image, VkImageView view)
        {
            _device = device;
            Image = image;
            View = view;
        }

        public void Dispose()
        {
            if (View.Handle != 0)
            {
                _device._deviceApi.vkDestroyImageView(_device._device, View);
                View = default;
            }
        }
    }

    private sealed class VulkanSampler : ISampler
    {
        private readonly GraphicsDevice _device;
        public SamplerDesc Description { get; }
        internal VkSampler Sampler;

        public VulkanSampler(GraphicsDevice device, SamplerDesc desc, VkSampler sampler)
        {
            _device = device;
            Description = desc;
            Sampler = sampler;
        }

        public void Dispose()
        {
            if (Sampler.Handle != 0)
            {
                _device._deviceApi.vkDestroySampler(_device._device, Sampler);
                Sampler = default;
            }
        }
    }

    public IImage CreateImage(ImageDesc desc)
    {
        VkImageCreateInfo imageInfo = new()
        {
            imageType = VkImageType.Image2D,
            format = ToVkFormat(desc.Format),
            extent = new VkExtent3D(desc.Extent.Width, desc.Extent.Height, 1),
            mipLevels = 1,
            arrayLayers = 1,
            samples = VkSampleCountFlags.Count1,
            tiling = VkImageTiling.Optimal,
            usage = ToVkImageUsage(desc.Usage),
            sharingMode = VkSharingMode.Exclusive,
            initialLayout = VkImageLayout.Undefined
        };

        _deviceApi.vkCreateImage(_device, &imageInfo, null, out VkImage image).CheckResult();
        _deviceApi.vkGetImageMemoryRequirements(_device, image, out VkMemoryRequirements req);

        VkMemoryAllocateInfo allocInfo = new()
        {
            allocationSize = req.size,
            memoryTypeIndex = FindMemoryType(req.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal)
        };

        _deviceApi.vkAllocateMemory(_device, &allocInfo, null, out VkDeviceMemory memory).CheckResult();
        _deviceApi.vkBindImageMemory(_device, image, memory, 0).CheckResult();

        return new VulkanImage(this, image, memory, desc);
    }

    public IImageView CreateImageView(IImage image)
    {
        if (image is not VulkanImage vkImage)
            throw new ArgumentException("Image was not created by this device.", nameof(image));

        VkImageAspectFlags aspect = vkImage.Description.Format switch
        {
            ImageFormat.D24_UNorm_S8_UInt => VkImageAspectFlags.Depth | VkImageAspectFlags.Stencil,
            ImageFormat.D32_Float          => VkImageAspectFlags.Depth,
            _                             => VkImageAspectFlags.Color
        };

        VkImageViewCreateInfo viewInfo = new()
        {
            image = vkImage.Image,
            viewType = VkImageViewType.Image2D,
            format = ToVkFormat(vkImage.Description.Format),
            components = VkComponentMapping.Rgba,
            subresourceRange = new VkImageSubresourceRange(aspect, 0, 1, 0, 1)
        };

        _deviceApi.vkCreateImageView(_device, &viewInfo, null, out VkImageView view).CheckResult();
        return new VulkanImageView(this, image, view);
    }

    public ISampler CreateSampler(SamplerDesc desc)
    {
        VkSamplerCreateInfo info = new()
        {
            magFilter = desc.MagFilter == SamplerFilter.Linear ? VkFilter.Linear : VkFilter.Nearest,
            minFilter = desc.MinFilter == SamplerFilter.Linear ? VkFilter.Linear : VkFilter.Nearest,
            addressModeU = ToVkAddressMode(desc.AddressU),
            addressModeV = ToVkAddressMode(desc.AddressV),
            addressModeW = ToVkAddressMode(desc.AddressW),
            anisotropyEnable = false,
            borderColor = VkBorderColor.IntOpaqueBlack,
            unnormalizedCoordinates = false,
            compareEnable = false,
            compareOp = VkCompareOp.Always,
            mipmapMode = VkSamplerMipmapMode.Linear
        };

        _deviceApi.vkCreateSampler(_device, &info, null, out VkSampler sampler).CheckResult();
        return new VulkanSampler(this, desc, sampler);
    }

    private static VkSamplerAddressMode ToVkAddressMode(SamplerAddressMode mode) => mode switch
    {
        SamplerAddressMode.ClampToEdge => VkSamplerAddressMode.ClampToEdge,
        SamplerAddressMode.MirrorRepeat => VkSamplerAddressMode.MirroredRepeat,
        SamplerAddressMode.Repeat => VkSamplerAddressMode.Repeat,
        _ => VkSamplerAddressMode.ClampToEdge
    };

    private static VkFormat ToVkFormat(ImageFormat format) => format switch
    {
        ImageFormat.R8G8B8A8_UNorm => VkFormat.R8G8B8A8Unorm,
        ImageFormat.B8G8R8A8_UNorm => VkFormat.B8G8R8A8Unorm,
        ImageFormat.D24_UNorm_S8_UInt => VkFormat.D24UnormS8Uint,
        ImageFormat.D32_Float => VkFormat.D32Sfloat,
        _ => VkFormat.Undefined
    };

    private static VkImageUsageFlags ToVkImageUsage(ImageUsage usage)
    {
        VkImageUsageFlags flags = 0;
        if (usage.HasFlag(ImageUsage.ColorAttachment)) flags |= VkImageUsageFlags.ColorAttachment;
        if (usage.HasFlag(ImageUsage.DepthStencilAttachment)) flags |= VkImageUsageFlags.DepthStencilAttachment;
        if (usage.HasFlag(ImageUsage.Sampled)) flags |= VkImageUsageFlags.Sampled;
        if (usage.HasFlag(ImageUsage.TransferSrc)) flags |= VkImageUsageFlags.TransferSrc;
        if (usage.HasFlag(ImageUsage.TransferDst)) flags |= VkImageUsageFlags.TransferDst;
        return flags;
    }

    internal void TransitionImageLayout(IImage image, VkImageLayout oldLayout, VkImageLayout newLayout, VkImageAspectFlags aspect)
    {
        if (image is not VulkanImage vkImage)
            throw new ArgumentException("Image was not created by this device.", nameof(image));

        var cmd = BeginSingleTimeCommands();

        VkImageMemoryBarrier barrier = new()
        {
            oldLayout = oldLayout,
            newLayout = newLayout,
            srcQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
            image = vkImage.Image,
            subresourceRange = new VkImageSubresourceRange(aspect, 0, 1, 0, 1)
        };

        VkPipelineStageFlags srcStage = VkPipelineStageFlags.TopOfPipe;
        VkPipelineStageFlags dstStage = VkPipelineStageFlags.BottomOfPipe;

        _deviceApi.vkCmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrier);
        EndSingleTimeCommands(cmd);
        vkImage.Layout = newLayout;
    }

    internal void TransitionImageLayout(IImage image,
        VkImageLayout oldLayout,
        VkImageLayout newLayout,
        VkImageAspectFlags aspect,
        VkPipelineStageFlags srcStage,
        VkPipelineStageFlags dstStage)
    {
        if (image is not VulkanImage vkImage)
            throw new ArgumentException("Image was not created by this device.", nameof(image));

        var cmd = BeginSingleTimeCommands();

        VkImageMemoryBarrier barrier = new()
        {
            oldLayout = oldLayout,
            newLayout = newLayout,
            srcQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
            image = vkImage.Image,
            subresourceRange = new VkImageSubresourceRange(aspect, 0, 1, 0, 1)
        };

        _deviceApi.vkCmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrier);
        EndSingleTimeCommands(cmd);
        vkImage.Layout = newLayout;
    }

    internal void UploadTexture2D(IImage image, ReadOnlySpan<byte> data, uint width, uint height, int bytesPerPixel)
    {
        if (image is not VulkanImage vkImage)
            throw new ArgumentException("Image was not created by this device.", nameof(image));
        if (data.Length == 0) return;

        ulong expectedSize = (ulong)width * height * (ulong)bytesPerPixel;
        if ((ulong)data.Length < expectedSize)
            throw new ArgumentException("Provided data is smaller than the expected image size.", nameof(data));

        // Create staging buffer
        var stagingDesc = new BufferDesc(expectedSize, BufferUsage.TransferSrc, CpuAccessMode.Write);
        var staging = (VulkanBuffer)CreateBuffer(stagingDesc);
        try
        {
            var stagingSpan = Map(staging);
            data.Slice(0, (int)expectedSize).CopyTo(stagingSpan);
            Unmap(staging);

            var cmd = BeginSingleTimeCommands();

            // Transition image to transfer dst
            VkImageMemoryBarrier barrierToDst = new()
            {
                oldLayout = vkImage.Layout,
                newLayout = VkImageLayout.TransferDstOptimal,
                srcQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
                dstQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
                image = vkImage.Image,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
            };

            VkPipelineStageFlags srcStage = VkPipelineStageFlags.TopOfPipe;
            VkPipelineStageFlags dstStage = VkPipelineStageFlags.Transfer;

            _deviceApi.vkCmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrierToDst);

            // Copy buffer to image
            VkBufferImageCopy region = new()
            {
                bufferOffset = 0,
                bufferRowLength = 0,
                bufferImageHeight = 0,
                imageSubresource = new VkImageSubresourceLayers(VkImageAspectFlags.Color, 0, 0, 1),
                imageOffset = new VkOffset3D(0, 0, 0),
                imageExtent = new VkExtent3D(width, height, 1)
            };

            _deviceApi.vkCmdCopyBufferToImage(cmd, staging.Buffer, vkImage.Image, VkImageLayout.TransferDstOptimal, 1, &region);

            // Transition image to shader read-only
            VkImageMemoryBarrier barrierToShaderRead = new()
            {
                oldLayout = VkImageLayout.TransferDstOptimal,
                newLayout = VkImageLayout.ShaderReadOnlyOptimal,
                srcQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
                dstQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
                image = vkImage.Image,
                subresourceRange = new VkImageSubresourceRange(VkImageAspectFlags.Color, 0, 1, 0, 1)
            };

            srcStage = VkPipelineStageFlags.Transfer;
            dstStage = VkPipelineStageFlags.FragmentShader;

            _deviceApi.vkCmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 0, null, 1, &barrierToShaderRead);

            _deviceApi.vkEndCommandBuffer(cmd).CheckResult();

            VkSubmitInfo submitInfo = new()
            {
                commandBufferCount = 1,
                pCommandBuffers = &cmd
            };

            VkFence fence;
            VkFenceCreateInfo fenceInfo = new();
            _deviceApi.vkCreateFence(_device, &fenceInfo, null, out fence).CheckResult();

            _deviceApi.vkQueueSubmit(_graphicsQueue, 1, &submitInfo, fence).CheckResult();
            _deviceApi.vkWaitForFences(_device, 1, &fence, true, ulong.MaxValue).CheckResult();

            _deviceApi.vkDestroyFence(_device, fence);
            _deviceApi.vkFreeCommandBuffers(_device, _commandPool, 1, &cmd);

            vkImage.Layout = VkImageLayout.ShaderReadOnlyOptimal;
        }
        finally
        {
            staging.Dispose();
        }
    }

    // Convenience texture upload overloads ------------------------------------------------------

    internal void UploadTexture2D<TPixel>(IImage image, ReadOnlySpan<TPixel> pixels, uint width, uint height)
        where TPixel : unmanaged
    {
        int bpp = sizeof(TPixel);
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(pixels);
        ulong expectedSize = (ulong)width * height * (ulong)bpp;
        if ((ulong)bytes.Length < expectedSize)
            throw new ArgumentException("Pixel span is smaller than expected image size.", nameof(pixels));

        UploadTexture2D(image, bytes, width, height, bpp);
    }

    IImage IGraphicsDevice.CreateImage(ImageDesc desc) => CreateImage(desc);
    IImageView IGraphicsDevice.CreateImageView(IImage image) => CreateImageView(image);
    ISampler IGraphicsDevice.CreateSampler(SamplerDesc desc) => CreateSampler(desc);
}
