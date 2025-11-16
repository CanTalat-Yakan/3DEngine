using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Vortice.Vulkan;

namespace Engine;

// Buffer abstractions and Vulkan-backed implementation.
public sealed unsafe partial class GraphicsDevice
{
    // In a later step we will introduce a VMA allocator; for now keep the implementation minimal and
    // use raw Vulkan buffer + device allocation. This already exercises the API surface.

    private sealed class VulkanBuffer : IBuffer
    {
        private readonly GraphicsDevice _device;
        internal VkBuffer Buffer;
        internal VkDeviceMemory Memory;
        public BufferDesc Description { get; }
        internal bool IsHostVisible;
        internal nint MappedPtr;

        public VulkanBuffer(GraphicsDevice device, VkBuffer buffer, VkDeviceMemory memory, BufferDesc desc, bool hostVisible)
        {
            _device = device;
            Buffer = buffer;
            Memory = memory;
            Description = desc;
            IsHostVisible = hostVisible;
            MappedPtr = nint.Zero;
        }

        public void Dispose()
        {
            if (Buffer.Handle != 0)
            {
                _device._deviceApi.vkDestroyBuffer(_device._device, Buffer);
                Buffer = default;
            }

            if (Memory.Handle != 0)
            {
                _device._deviceApi.vkFreeMemory(_device._device, Memory);
                Memory = default;
            }
        }
    }

    IBuffer IGraphicsDevice.CreateBuffer(BufferDesc desc) => CreateBuffer(desc);
    Span<byte> IGraphicsDevice.Map(IBuffer buffer) => Map(buffer);
    void IGraphicsDevice.Unmap(IBuffer buffer) => Unmap(buffer);

    public IBuffer CreateBuffer(BufferDesc desc)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Graphics device not initialized");
        if (desc.Size == 0)
            throw new ArgumentOutOfRangeException(nameof(desc), "Buffer size must be greater than zero.");

        VkBufferUsageFlags usage = 0;
        if (desc.Usage.HasFlag(BufferUsage.Vertex)) usage |= VkBufferUsageFlags.VertexBuffer;
        if (desc.Usage.HasFlag(BufferUsage.Index)) usage |= VkBufferUsageFlags.IndexBuffer;
        if (desc.Usage.HasFlag(BufferUsage.Uniform)) usage |= VkBufferUsageFlags.UniformBuffer;
        if (desc.Usage.HasFlag(BufferUsage.TransferSrc)) usage |= VkBufferUsageFlags.TransferSrc;
        if (desc.Usage.HasFlag(BufferUsage.TransferDst)) usage |= VkBufferUsageFlags.TransferDst;

        VkBufferCreateInfo bufferInfo = new()
        {
            size = desc.Size,
            usage = usage,
            sharingMode = VkSharingMode.Exclusive
        };

        _deviceApi.vkCreateBuffer(_device, &bufferInfo, null, out VkBuffer buffer).CheckResult();

        _deviceApi.vkGetBufferMemoryRequirements(_device, buffer, out VkMemoryRequirements requirements);

        var properties = desc.CpuAccess == CpuAccessMode.None
            ? VkMemoryPropertyFlags.DeviceLocal
            : VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent;

        uint memoryTypeIndex = FindMemoryType(requirements.memoryTypeBits, properties);

        VkMemoryAllocateInfo allocInfo = new()
        {
            allocationSize = requirements.size,
            memoryTypeIndex = memoryTypeIndex
        };

        _deviceApi.vkAllocateMemory(_device, &allocInfo, null, out VkDeviceMemory memory).CheckResult();
        _deviceApi.vkBindBufferMemory(_device, buffer, memory, 0).CheckResult();

        return new VulkanBuffer(this, buffer, memory, desc, desc.CpuAccess != CpuAccessMode.None);
    }

    public Span<byte> Map(IBuffer buffer)
    {
        if (buffer is not VulkanBuffer vkBuffer)
            throw new ArgumentException("Buffer was not created by this device.", nameof(buffer));

        if (!vkBuffer.IsHostVisible)
            throw new InvalidOperationException("Buffer is not host-visible and cannot be mapped.");

        if (vkBuffer.MappedPtr != nint.Zero)
        {
            // Already mapped; just return the same span.
            return new Span<byte>((void*)vkBuffer.MappedPtr, checked((int)vkBuffer.Description.Size));
        }

        void* data;
        _deviceApi.vkMapMemory(_device, vkBuffer.Memory, 0, vkBuffer.Description.Size, 0, &data).CheckResult();
        vkBuffer.MappedPtr = (nint)data;
        return new Span<byte>(data, checked((int)vkBuffer.Description.Size));
    }

    public void Unmap(IBuffer buffer)
    {
        if (buffer is not VulkanBuffer vkBuffer)
            throw new ArgumentException("Buffer was not created by this device.", nameof(buffer));

        if (!vkBuffer.IsHostVisible || vkBuffer.MappedPtr == nint.Zero)
            return;

        _deviceApi.vkUnmapMemory(_device, vkBuffer.Memory);
        vkBuffer.MappedPtr = nint.Zero;
    }

    public void UploadBuffer(IBuffer destination, ReadOnlySpan<byte> data, ulong destinationOffset = 0)
    {
        if (destination is not VulkanBuffer dst)
            throw new ArgumentException("Buffer was not created by this device.", nameof(destination));
        if (data.Length == 0) return;

        // If the destination is host-visible, just map and copy directly.
        if (dst.IsHostVisible)
        {
            var span = Map(dst);
            data.CopyTo(span.Slice((int)destinationOffset));
            Unmap(dst);
            return;
        }

        // Device-local: create a transient staging buffer and copy via one-time command buffer.
        var stagingDesc = new BufferDesc((ulong)data.Length, BufferUsage.TransferSrc, CpuAccessMode.Write);
        var staging = (VulkanBuffer)CreateBuffer(stagingDesc);
        try
        {
            var stagingSpan = Map(staging);
            data.CopyTo(stagingSpan);
            Unmap(staging);

            VkBufferCopy copyRegion = new()
            {
                srcOffset = 0,
                dstOffset = destinationOffset,
                size = (ulong)data.Length
            };

            var cmd = BeginSingleTimeCommands();
            _deviceApi.vkCmdCopyBuffer(cmd, staging.Buffer, dst.Buffer, 1, &copyRegion);
            EndSingleTimeCommands(cmd);
        }
        finally
        {
            staging.Dispose();
        }
    }

    // Generic convenience upload helpers ---------------------------------------------------------

    public void Upload<T>(IBuffer destination, ReadOnlySpan<T> data, ulong destinationOffsetBytes = 0)
        where T : unmanaged
    {
        if (data.IsEmpty) return;

        ulong elementSize = (ulong)Unsafe.SizeOf<T>();
        ulong totalSize = elementSize * (ulong)data.Length;

        // reinterpret the span as bytes
        ReadOnlySpan<byte> bytes = MemoryMarshal.AsBytes(data);
        if ((ulong)bytes.Length < totalSize)
            throw new ArgumentException("Span length is smaller than expected total size.", nameof(data));

        UploadBuffer(destination, bytes, destinationOffsetBytes);
    }

    public void Upload<T>(IBuffer destination, T value, ulong destinationOffsetBytes = 0)
        where T : unmanaged
    {
        Span<T> temp = stackalloc T[1];
        temp[0] = value;
        Upload(destination, temp, destinationOffsetBytes);
    }

    public void Upload<T>(IBuffer destination, T[] data, ulong destinationOffsetBytes = 0)
        where T : unmanaged
    {
        if (data.Length == 0) return;
        Upload(destination, data.AsSpan(), destinationOffsetBytes);
    }

    // Minimal buffer barrier helper --------------------------------------------------------------

    internal void BufferMemoryBarrier(IBuffer buffer,
        VkAccessFlags srcAccess,
        VkAccessFlags dstAccess,
        VkPipelineStageFlags srcStage,
        VkPipelineStageFlags dstStage)
    {
        if (buffer is not VulkanBuffer vkBuffer)
            throw new ArgumentException("Buffer was not created by this device.", nameof(buffer));

        var cmd = BeginSingleTimeCommands();

        VkBufferMemoryBarrier barrier = new()
        {
            srcAccessMask = srcAccess,
            dstAccessMask = dstAccess,
            srcQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
            dstQueueFamilyIndex = Vulkan.VK_QUEUE_FAMILY_IGNORED,
            buffer = vkBuffer.Buffer,
            offset = 0,
            size = vkBuffer.Description.Size
        };

        _deviceApi.vkCmdPipelineBarrier(cmd, srcStage, dstStage, 0, 0, null, 1, &barrier, 0, null);

        EndSingleTimeCommands(cmd);
    }

    private VkCommandBuffer BeginSingleTimeCommands()
    {
        VkCommandBufferAllocateInfo allocInfo = new()
        {
            commandPool = _commandPool,
            level = VkCommandBufferLevel.Primary,
            commandBufferCount = 1
        };

        VkCommandBuffer cmd;
        _deviceApi.vkAllocateCommandBuffers(_device, &allocInfo, &cmd).CheckResult();

        VkCommandBufferBeginInfo beginInfo = new()
        {
            flags = VkCommandBufferUsageFlags.OneTimeSubmit
        };

        _deviceApi.vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();
        return cmd;
    }

    private void EndSingleTimeCommands(VkCommandBuffer cmd)
    {
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
    }

    private uint FindMemoryType(uint typeFilter, VkMemoryPropertyFlags properties)
    {
        VkPhysicalDeviceMemoryProperties memProperties;
        _instanceApi.vkGetPhysicalDeviceMemoryProperties(_physicalDevice, out memProperties);

        for (int i = 0; i < memProperties.memoryTypeCount; i++)
        {
            if ((typeFilter & (1u << i)) != 0 &&
                (memProperties.memoryTypes[i].propertyFlags & properties) == properties)
            {
                return (uint)i;
            }
        }

        throw new InvalidOperationException("Failed to find suitable memory type.");
    }
}
