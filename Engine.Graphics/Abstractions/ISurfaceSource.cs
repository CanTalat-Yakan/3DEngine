namespace Engine;

/// <summary>Platform-specific surface provider for Vulkan instance/surface creation.</summary>
public interface ISurfaceSource
{
    IReadOnlyList<string> GetRequiredInstanceExtensions();
    nint CreateSurfaceHandle(nint instanceHandle);
    (uint Width, uint Height) GetDrawableSize();
}

