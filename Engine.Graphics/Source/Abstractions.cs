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
