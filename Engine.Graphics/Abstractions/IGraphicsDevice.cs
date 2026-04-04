namespace Engine;

/// <summary>Primary graphics device interface for resource creation and frame management.</summary>
public interface IGraphicsDevice : IDisposable
{
    bool IsInitialized { get; }
    ISwapchain Swapchain { get; }
    GraphicsAdapterInfo AdapterInfo { get; }

    /// <summary>Total number of frames that may be in flight simultaneously.</summary>
    int FramesInFlight { get; }

    void Initialize(ISurfaceSource surfaceSource, string appName = "3DEngine");
    IFrameContext BeginFrame(ClearColor? clearOverride = null);
    void EndFrame(IFrameContext frameContext);
    void OnResize();

    // Minimal buffer API
    IBuffer CreateBuffer(BufferDesc desc);
    Span<byte> Map(IBuffer buffer);
    void Unmap(IBuffer buffer);

    // Images / samplers
    IImage CreateImage(ImageDesc desc);
    IImageView CreateImageView(IImage image);
    ISampler CreateSampler(SamplerDesc desc);

    // Shaders / pipelines
    IShader CreateShader(ShaderDesc desc);
    IPipeline CreateGraphicsPipeline(GraphicsPipelineDesc desc);

    // Descriptors (single set for camera UBO + optional sampler)
    IDescriptorSet CreateDescriptorSet();

    // Update a descriptor set with optional uniform buffer and combined image sampler bindings.
    void UpdateDescriptorSet(IDescriptorSet descriptorSet, in UniformBufferBinding? uniformBinding, in CombinedImageSamplerBinding? samplerBinding);

    // Minimal draw API for forward pass: bind pipeline, descriptor set, and issue draws.
    void BindGraphicsPipeline(ICommandBuffer commandBuffer, IPipeline pipeline);
    void BindDescriptorSet(ICommandBuffer commandBuffer, IPipeline pipeline, IDescriptorSet descriptorSet);
    void Draw(ICommandBuffer commandBuffer, uint vertexCount, uint instanceCount = 1, uint firstVertex = 0, uint firstInstance = 0);
    void DrawIndexed(ICommandBuffer commandBuffer, uint indexCount, uint instanceCount = 1, uint firstIndex = 0, int vertexOffset = 0, uint firstInstance = 0);

    // Vertex/index buffer binding
    void BindVertexBuffers(ICommandBuffer commandBuffer, uint firstBinding, IBuffer[] buffers, ulong[] offsets);
    void BindIndexBuffer(ICommandBuffer commandBuffer, IBuffer buffer, ulong offset, IndexType indexType);

    // Dynamic state
    void SetViewport(ICommandBuffer commandBuffer, float x, float y, float width, float height, float minDepth, float maxDepth);
    void SetScissor(ICommandBuffer commandBuffer, int x, int y, uint width, uint height);

    // Push constants
    void PushConstants(ICommandBuffer commandBuffer, IPipeline pipeline, ShaderStageFlags stageFlags, uint offset, ReadOnlySpan<byte> data);

    // Texture upload
    void UploadTexture2D(IImage image, ReadOnlySpan<byte> data, uint width, uint height, int bytesPerPixel);
}

