using Vortice.Vulkan;

namespace Engine;

public sealed unsafe partial class GraphicsDevice
{
    private partial IFrameContext BeginFrameInternal(ClearColor clearColor)
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Graphics device not initialized");

        _deviceApi.vkWaitForFences(_device, _inFlightFences[_currentFrame], true, ulong.MaxValue).CheckResult();

        var result = _deviceApi.vkAcquireNextImageKHR(_device, _swapchain, ulong.MaxValue,
            _imageAvailableSemaphores[_currentFrame], default, out uint imageIndex);

        if (result == VkResult.ErrorOutOfDateKHR)
        {
            OnResize();
            return BeginFrameInternal(clearColor);
        }

        if (result != VkResult.SuboptimalKHR)
            result.CheckResult();

        _lastAcquiredImageIndex = imageIndex;
        var cmd = _commandBuffers[_currentFrame];

        VkCommandBufferBeginInfo beginInfo = new();
        _deviceApi.vkResetCommandBuffer(cmd, 0).CheckResult();
        _deviceApi.vkBeginCommandBuffer(cmd, &beginInfo).CheckResult();

        var clearValue = new VkClearValue(new VkClearColorValue(clearColor.R, clearColor.G, clearColor.B, clearColor.A));
        VkRenderPassBeginInfo rpBegin = new()
        {
            renderPass = _renderPass,
            framebuffer = _framebuffers[imageIndex],
            renderArea = new VkRect2D(new VkOffset2D(0, 0), _swapchainExtent),
            clearValueCount = 1,
            pClearValues = &clearValue
        };

        _deviceApi.vkCmdBeginRenderPass(cmd, &rpBegin, VkSubpassContents.Inline);

        return new VulkanFrameContext(this, imageIndex, cmd, _swapchainExtent, _renderPass, _framebuffers[imageIndex], _resizeVersion);
    }

    private partial void SubmitFrame(VulkanFrameContext ctx)
    {
        _deviceApi.vkCmdEndRenderPass(ctx.CommandBufferHandle);
        _deviceApi.vkEndCommandBuffer(ctx.CommandBufferHandle).CheckResult();

        var waitStage = VkPipelineStageFlags.ColorAttachmentOutput;
        VkSemaphore* waitSemaphores = stackalloc VkSemaphore[1];
        waitSemaphores[0] = _imageAvailableSemaphores[_currentFrame];
        VkCommandBuffer* commandBuffers = stackalloc VkCommandBuffer[1];
        commandBuffers[0] = ctx.CommandBufferHandle;
        VkSemaphore* signalSemaphores = stackalloc VkSemaphore[1];
        signalSemaphores[0] = _renderFinishedSemaphores[_currentFrame];

        VkSubmitInfo submitInfo = new()
        {
            waitSemaphoreCount = 1,
            pWaitSemaphores = waitSemaphores,
            pWaitDstStageMask = &waitStage,
            commandBufferCount = 1,
            pCommandBuffers = commandBuffers,
            signalSemaphoreCount = 1,
            pSignalSemaphores = signalSemaphores
        };

        _deviceApi.vkResetFences(_device, _inFlightFences[_currentFrame]).CheckResult();
        _deviceApi.vkQueueSubmit(_graphicsQueue, 1, &submitInfo, _inFlightFences[_currentFrame]).CheckResult();

        VkSemaphore* presentWaitSemaphores = stackalloc VkSemaphore[1];
        presentWaitSemaphores[0] = _renderFinishedSemaphores[_currentFrame];
        VkSwapchainKHR* swapchains = stackalloc VkSwapchainKHR[1];
        swapchains[0] = _swapchain;
        uint* imageIndices = stackalloc uint[1];
        imageIndices[0] = ctx.FrameIndex;

        VkPresentInfoKHR presentInfo = new()
        {
            waitSemaphoreCount = 1,
            pWaitSemaphores = presentWaitSemaphores,
            swapchainCount = 1,
            pSwapchains = swapchains,
            pImageIndices = imageIndices
        };

        var presentResult = _deviceApi.vkQueuePresentKHR(_presentQueue, &presentInfo);

        if (presentResult == VkResult.ErrorOutOfDateKHR || presentResult == VkResult.SuboptimalKHR)
        {
            OnResize();
        }
        else
        {
            presentResult.CheckResult();
        }

        _currentFrame = (_currentFrame + 1) % MaxFramesInFlight;
    }

    private sealed class VulkanFrameContext : IFrameContext
    {
        private readonly GraphicsDevice _owner;
        private readonly ulong _bornResizeVersion;

        public uint FrameIndex { get; }
        public ICommandBuffer CommandBuffer { get; }
        public IRenderPass RenderPass { get; }
        public IFramebuffer Framebuffer { get; }
        public Extent2D Extent { get; }

        internal VkCommandBuffer CommandBufferHandle { get; }

        public VulkanFrameContext(GraphicsDevice owner, uint frameIndex, VkCommandBuffer cmd, VkExtent2D extent,
            VkRenderPass renderPass, VkFramebuffer framebuffer, ulong resizeVersion)
        {
            _owner = owner;
            _bornResizeVersion = resizeVersion;
            FrameIndex = frameIndex;
            CommandBufferHandle = cmd;
            Extent = new Extent2D(extent.width, extent.height);
            RenderPass = new VulkanRenderPass(renderPass);
            Framebuffer = new VulkanFramebuffer(framebuffer);
            CommandBuffer = new VulkanCommandBuffer(cmd);
        }

        public void Dispose()
        {
            if (_bornResizeVersion != _owner._resizeVersion)
            {
                // resources already destroyed with swapchain recreation
            }
        }
    }

    private sealed class VulkanCommandBuffer : ICommandBuffer
    {
        internal VkCommandBuffer Handle { get; }
        public VulkanCommandBuffer(VkCommandBuffer handle) => Handle = handle;
    }

    private sealed class VulkanRenderPass : IRenderPass
    {
        internal VkRenderPass Handle { get; }
        public VulkanRenderPass(VkRenderPass handle) => Handle = handle;
    }

    private sealed class VulkanFramebuffer : IFramebuffer
    {
        internal VkFramebuffer Handle { get; }
        public VulkanFramebuffer(VkFramebuffer handle) => Handle = handle;
    }

    private sealed class VulkanSwapchain : ISwapchain
    {
        private readonly GraphicsDevice _owner;
        public VulkanSwapchain(GraphicsDevice owner) => _owner = owner;

        public Extent2D Extent => new(_owner._swapchainExtent.width, _owner._swapchainExtent.height);
        public uint ImageCount => (uint)_owner._swapchainImages.Length;

        public AcquireResult AcquireNextImage(out uint imageIndex)
        {
            imageIndex = _owner._lastAcquiredImageIndex;
            return AcquireResult.Success;
        }

        public void Resize(Extent2D newExtent) => _owner.OnResize();
        public void Dispose() { }
    }
}
