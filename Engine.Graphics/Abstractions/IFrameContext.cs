namespace Engine;

/// <summary>Per-frame rendering context with command buffer and render pass.</summary>
public interface IFrameContext : IDisposable
{
    uint FrameIndex { get; }
    ICommandBuffer CommandBuffer { get; }
    IRenderPass RenderPass { get; }
    IFramebuffer Framebuffer { get; }
    Extent2D Extent { get; }
}

