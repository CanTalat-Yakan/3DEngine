namespace Engine;

/// <summary>Common value types and DTOs shared across the graphics abstraction layer.</summary>

/// <summary>RGBA clear color for render pass initialization.</summary>
/// <param name="R">Red channel (0–1).</param>
/// <param name="G">Green channel (0–1).</param>
/// <param name="B">Blue channel (0–1).</param>
/// <param name="A">Alpha channel (0–1).</param>
public readonly record struct ClearColor(float R, float G, float B, float A)
{
    /// <summary>Opaque black clear color.</summary>
    public static readonly ClearColor Black = new(0, 0, 0, 1);
}

/// <summary>Two-dimensional extent in pixels (width × height).</summary>
/// <param name="Width">Width in pixels.</param>
/// <param name="Height">Height in pixels.</param>
public readonly record struct Extent2D(uint Width, uint Height);

/// <summary>GPU device type classification, matching Vulkan physical device types.</summary>
public enum GraphicsDeviceType
{
    /// <summary>Device type cannot be determined.</summary>
    Unknown,
    /// <summary>Integrated GPU sharing system memory with the CPU.</summary>
    IntegratedGpu,
    /// <summary>Discrete GPU with dedicated video memory.</summary>
    DiscreteGpu,
    /// <summary>Virtual GPU in a virtualization environment.</summary>
    VirtualGpu,
    /// <summary>CPU-only software rasterizer.</summary>
    Cpu,
    /// <summary>Software-based rendering backend (e.g., SDL software renderer).</summary>
    Software
}

/// <summary>Information about the selected graphics adapter (GPU).</summary>
/// <param name="Name">Human-readable adapter name.</param>
/// <param name="VendorId">PCI vendor identifier.</param>
/// <param name="DeviceId">PCI device identifier.</param>
/// <param name="DeviceType">Device classification (discrete, integrated, etc.).</param>
public readonly record struct GraphicsAdapterInfo(string Name, uint VendorId, uint DeviceId, GraphicsDeviceType DeviceType)
{
    /// <summary>Placeholder info for uninitialized or unknown adapters.</summary>
    public static readonly GraphicsAdapterInfo Unknown = new("Unknown", 0, 0, GraphicsDeviceType.Unknown);
}
