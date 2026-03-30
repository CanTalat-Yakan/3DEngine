namespace Engine;

/// <summary>Primary graphics device interface for resource creation and frame management.</summary>
public interface IGraphicsDevice : IDisposable
{
    bool IsInitialized { get; }
    ISwapchain Swapchain { get; }
    GraphicsAdapterInfo AdapterInfo { get; }

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
}

