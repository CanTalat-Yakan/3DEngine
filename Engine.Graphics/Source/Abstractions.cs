namespace Engine;

// High-level, API-agnostic graphics abstractions used by Engine.Renderer and other systems.
// These interfaces must not expose Vulkan (or any specific graphics API) types.

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

public interface ISwapchain : IDisposable
{
    Extent2D Extent { get; }
    uint ImageCount { get; }

    AcquireResult AcquireNextImage(out uint imageIndex);
    void Resize(Extent2D newExtent);
}

public interface IFrameContext : IDisposable
{
    uint FrameIndex { get; }
    ICommandBuffer CommandBuffer { get; }
    IRenderPass RenderPass { get; }
    IFramebuffer Framebuffer { get; }
    Extent2D Extent { get; }
}

public interface ISurfaceSource
{
    IReadOnlyList<string> GetRequiredInstanceExtensions();
    nint CreateSurfaceHandle(nint instanceHandle); // abstract raw surface creation
    (uint Width, uint Height) GetDrawableSize();
}

// Resource/command interfaces kept deliberately minimal for now; they can grow as needed.
public interface IFramebuffer { }
public interface IRenderPass { }
public interface IPipeline { }
public interface ICommandBuffer { }

public enum AcquireResult
{
    Success,
    OutOfDate,
    Suboptimal,
    Error
}

// Basic value types / DTOs
public readonly record struct ClearColor(float R, float G, float B, float A)
{
    public static readonly ClearColor Black = new(0, 0, 0, 1);
}

public readonly record struct Extent2D(uint Width, uint Height);

public enum GraphicsDeviceType
{
    Unknown,
    IntegratedGpu,
    DiscreteGpu,
    VirtualGpu,
    Cpu,
    Software
}

public readonly record struct GraphicsAdapterInfo(string Name, uint VendorId, uint DeviceId, GraphicsDeviceType DeviceType)
{
    public static readonly GraphicsAdapterInfo Unknown = new("Unknown", 0, 0, GraphicsDeviceType.Unknown);
}

// Buffer abstractions
[Flags]
public enum BufferUsage
{
    None = 0,
    Vertex = 1 << 0,
    Index = 1 << 1,
    Uniform = 1 << 2,
    TransferSrc = 1 << 3,
    TransferDst = 1 << 4,
    Staging = 1 << 5
}

public enum CpuAccessMode
{
    None,
    Read,
    Write,
    ReadWrite
}

public readonly record struct BufferDesc(ulong Size, BufferUsage Usage, CpuAccessMode CpuAccess = CpuAccessMode.None);

public interface IBuffer : IDisposable
{
    BufferDesc Description { get; }
}

// Image / sampler abstractions
public enum ImageFormat
{
    Undefined,
    R8G8B8A8_UNorm,
    B8G8R8A8_UNorm,
    D24_UNorm_S8_UInt,
    D32_Float
}

[Flags]
public enum ImageUsage
{
    None          = 0,
    ColorAttachment      = 1 << 0,
    DepthStencilAttachment = 1 << 1,
    Sampled       = 1 << 2,
    TransferSrc   = 1 << 3,
    TransferDst   = 1 << 4
}

public readonly record struct ImageDesc(Extent2D Extent, ImageFormat Format, ImageUsage Usage);

public interface IImage : IDisposable
{
    ImageDesc Description { get; }
}

public interface IImageView : IDisposable
{
    IImage Image { get; }
}

public enum SamplerFilter
{
    Nearest,
    Linear
}

public enum SamplerAddressMode
{
    ClampToEdge,
    Repeat,
    MirrorRepeat
}

public readonly record struct SamplerDesc(
    SamplerFilter MinFilter,
    SamplerFilter MagFilter,
    SamplerAddressMode AddressU,
    SamplerAddressMode AddressV,
    SamplerAddressMode AddressW);

public interface ISampler : IDisposable
{
    SamplerDesc Description { get; }
}

// Shader / pipeline abstractions
public enum ShaderStage
{
    Vertex,
    Fragment
}

public readonly record struct ShaderDesc(ShaderStage Stage, ReadOnlyMemory<byte> Bytecode, string EntryPoint = "main");

public interface IShader : IDisposable
{
    ShaderDesc Description { get; }
}

public readonly record struct GraphicsPipelineDesc(IRenderPass RenderPass, IShader VertexShader, IShader FragmentShader);

// Descriptor abstractions (minimal UBO + combined image sampler)
public interface IDescriptorSet : IDisposable { }

// Minimal binding descriptions used by internal helpers and higher layers when updating descriptor sets.
public readonly record struct UniformBufferBinding(IBuffer Buffer, uint Binding, ulong Offset, ulong Size);
public readonly record struct CombinedImageSamplerBinding(IImageView ImageView, ISampler Sampler, uint Binding);
