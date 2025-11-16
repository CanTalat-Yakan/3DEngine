using SDL3;

namespace Engine;

internal sealed class SdlSurfaceSource : ISurfaceSource
{
    private readonly SdlWindow _sdl;

    public SdlSurfaceSource(SdlWindow sdl)
    {
        _sdl = sdl;
    }

    public IReadOnlyList<string> GetRequiredInstanceExtensions()
    {
        var names = SDL.VulkanGetInstanceExtensions(out var count);
        if (names is null || count == 0)
            throw new InvalidOperationException($"SDL.VulkanGetInstanceExtensions failed: {SDL.GetError()}");
        return names;
    }

    public nint CreateSurfaceHandle(nint instanceHandle)
    {
        if(!SDL.VulkanCreateSurface(_sdl.Window, instanceHandle, IntPtr.Zero, out var surface))
            throw new InvalidOperationException($"SDL.VulkanCreateSurface failed: {SDL.GetError()}");
        return surface;
    }
    
    public (uint Width, uint Height) GetDrawableSize()
    {
        uint w = (uint)Math.Max(1, _sdl.Width);
        uint h = (uint)Math.Max(1, _sdl.Height);
        return (w, h);
    }
}
