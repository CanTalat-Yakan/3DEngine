namespace Engine;

// Common value types and DTOs shared across the graphics abstraction layer.

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

