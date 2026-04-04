namespace Engine;

/// <summary>No-op graphics device for tests and headless runs.</summary>
public sealed class NullGraphicsDevice : IGraphicsDevice
{
    private sealed class NullSwapchain : ISwapchain
    {
        public Extent2D Extent { get; private set; } = new(1,1);
        public uint ImageCount => 1;
        public AcquireResult AcquireNextImage(out uint imageIndex) { imageIndex = 0; return AcquireResult.Success; }
        public void Resize(Extent2D newExtent) { Extent = newExtent; }
        public void Dispose() { }
    }
    private sealed class NullFrameContext : IFrameContext
    {
        public uint FrameIndex { get; }
        public int InFlightIndex { get; }
        public int FramesInFlight => 1;
        public ICommandBuffer CommandBuffer { get; } = new NullCommandBuffer();
        public IRenderPass RenderPass { get; } = new NullRenderPass();
        public IFramebuffer Framebuffer { get; } = new NullFramebuffer();
        public Extent2D Extent { get; }
        public NullFrameContext(uint idx) { FrameIndex = idx; InFlightIndex = (int)(idx % 1); Extent = new Extent2D(1,1); }
        public void Dispose() { }
    }
    private sealed class NullCommandBuffer : ICommandBuffer { }
    private sealed class NullRenderPass : IRenderPass { }
    private sealed class NullFramebuffer : IFramebuffer { }

    public bool IsInitialized { get; private set; }
    public ISwapchain Swapchain { get; } = new NullSwapchain();
    public int FramesInFlight => 1;
    private uint _frame;
    public void Initialize(ISurfaceSource surfaceSource, string appName = "3DEngine") { IsInitialized = true; }
    public IFrameContext BeginFrame(ClearColor? clearOverride = null) { _frame++; return new NullFrameContext(_frame); }
    public void EndFrame(IFrameContext frameContext) { }
    public void OnResize() { }
    public void Dispose() { }
    public GraphicsAdapterInfo AdapterInfo => GraphicsAdapterInfo.Unknown;

    public IBuffer CreateBuffer(BufferDesc desc) => throw new NotSupportedException("NullGraphicsDevice does not support buffer creation.");
    public Span<byte> Map(IBuffer buffer) => throw new NotSupportedException("NullGraphicsDevice does not support buffer mapping.");
    public void Unmap(IBuffer buffer) { }

    public IImage CreateImage(ImageDesc desc) => throw new NotSupportedException("NullGraphicsDevice does not support images.");
    public IImageView CreateImageView(IImage image) => throw new NotSupportedException("NullGraphicsDevice does not support image views.");
    public ISampler CreateSampler(SamplerDesc desc) => throw new NotSupportedException("NullGraphicsDevice does not support samplers.");

    public IShader CreateShader(ShaderDesc desc) => throw new NotSupportedException("NullGraphicsDevice does not support shaders.");
    public IPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc) => throw new NotSupportedException("NullGraphicsDevice does not support pipelines.");

    public IDescriptorSet CreateDescriptorSet() => throw new NotSupportedException("NullGraphicsDevice does not support descriptors.");
    public void UpdateDescriptorSet(IDescriptorSet descriptorSet, in UniformBufferBinding? uniformBinding, in CombinedImageSamplerBinding? samplerBinding)
        => throw new NotSupportedException("NullGraphicsDevice does not support descriptors.");

    public void BindGraphicsPipeline(ICommandBuffer commandBuffer, IPipeline pipeline)
        => throw new NotSupportedException("NullGraphicsDevice does not support graphics pipelines.");

    public void BindDescriptorSet(ICommandBuffer commandBuffer, IPipeline pipeline, IDescriptorSet descriptorSet)
        => throw new NotSupportedException("NullGraphicsDevice does not support descriptors.");

    public void Draw(ICommandBuffer commandBuffer, uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0)
        => throw new NotSupportedException("NullGraphicsDevice does not support drawing.");

    public void DrawIndexed(ICommandBuffer commandBuffer, uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0)
        => throw new NotSupportedException("NullGraphicsDevice does not support drawing.");

    public void BindVertexBuffers(ICommandBuffer commandBuffer, uint firstBinding, IBuffer[] buffers, ulong[] offsets)
        => throw new NotSupportedException("NullGraphicsDevice does not support buffer binding.");

    public void BindIndexBuffer(ICommandBuffer commandBuffer, IBuffer buffer, ulong offset, IndexType indexType)
        => throw new NotSupportedException("NullGraphicsDevice does not support buffer binding.");

    public void SetViewport(ICommandBuffer commandBuffer, float x, float y, float width, float height, float minDepth, float maxDepth)
        => throw new NotSupportedException("NullGraphicsDevice does not support viewports.");

    public void SetScissor(ICommandBuffer commandBuffer, int x, int y, uint width, uint height)
        => throw new NotSupportedException("NullGraphicsDevice does not support scissors.");

    public void PushConstants(ICommandBuffer commandBuffer, IPipeline pipeline, ShaderStageFlags stageFlags, uint offset, ReadOnlySpan<byte> data)
        => throw new NotSupportedException("NullGraphicsDevice does not support push constants.");

    public void UploadTexture2D(IImage image, ReadOnlySpan<byte> data, uint width, uint height, int bytesPerPixel)
        => throw new NotSupportedException("NullGraphicsDevice does not support texture upload.");
}
