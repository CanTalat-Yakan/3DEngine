using System.Reflection;
using SDL3;
using Vortice.Vulkan;

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
        var sdlType = typeof(SDL);
        // Try variants in order: (nint window, out string[] names), (out string[] names), (nint window) returning string[]
        var m1 = sdlType.GetMethod("VulkanGetInstanceExtensions", new[] { typeof(nint), typeof(string[]).MakeByRefType() });
        if (m1 != null)
        {
            object[] args = { _sdl.Window, null! };
            var ok = (bool)m1.Invoke(null, args)!;
            var names = (string[]?)args[1];
            if (!ok || names == null || names.Length == 0)
                throw new InvalidOperationException($"SDL.VulkanGetInstanceExtensions failed: {SDL.GetError()}");
            return names;
        }
        var m2 = sdlType.GetMethod("VulkanGetInstanceExtensions", new[] { typeof(string[]).MakeByRefType() });
        if (m2 != null)
        {
            object[] args = { null! };
            var ok = (bool)m2.Invoke(null, args)!;
            var names = (string[]?)args[0];
            if (!ok || names == null || names.Length == 0)
                throw new InvalidOperationException($"SDL.VulkanGetInstanceExtensions failed: {SDL.GetError()}");
            return names;
        }
        var m3 = sdlType.GetMethod("VulkanGetInstanceExtensions", new[] { typeof(nint) });
        if (m3 != null)
        {
            var names = (string[]?)m3.Invoke(null, new object[] { _sdl.Window });
            if (names == null || names.Length == 0)
                throw new InvalidOperationException($"SDL.VulkanGetInstanceExtensions failed: {SDL.GetError()}");
            return names;
        }
        throw new MissingMethodException("SDL.VulkanGetInstanceExtensions signature not found.");
    }

    public VkSurfaceKHR CreateSurface(VkInstance instance)
    {
        var sdlType = typeof(SDL);
        // Try variants: (window, instance, out ulong surface, IntPtr allocator) -> bool
        var m1 = sdlType.GetMethod("VulkanCreateSurface", new[] { typeof(nint), typeof(nint), typeof(ulong).MakeByRefType(), typeof(IntPtr) });
        if (m1 != null)
        {
            object[] args = { _sdl.Window, (nint)instance.Handle, 0UL, IntPtr.Zero };
            var ok = (bool)m1.Invoke(null, args)!;
            var surface = (ulong)args[2];
            if (!ok || surface == 0)
                throw new InvalidOperationException($"SDL.VulkanCreateSurface failed: {SDL.GetError()}");
            return new VkSurfaceKHR(surface);
        }
        // (window, instance, out ulong surface) -> bool
        var m2 = sdlType.GetMethod("VulkanCreateSurface", new[] { typeof(nint), typeof(nint), typeof(ulong).MakeByRefType() });
        if (m2 != null)
        {
            object[] args = { _sdl.Window, (nint)instance.Handle, 0UL };
            var ok = (bool)m2.Invoke(null, args)!;
            var surface = (ulong)args[2];
            if (!ok || surface == 0)
                throw new InvalidOperationException($"SDL.VulkanCreateSurface failed: {SDL.GetError()}");
            return new VkSurfaceKHR(surface);
        }
        // (window, instance, IntPtr allocator) -> ulong
        var m3 = sdlType.GetMethod("VulkanCreateSurface", new[] { typeof(nint), typeof(nint), typeof(IntPtr) });
        if (m3 != null)
        {
            var surface = (ulong)(m3.Invoke(null, new object[] { _sdl.Window, (nint)instance.Handle, IntPtr.Zero }) ?? 0UL);
            if (surface == 0)
                throw new InvalidOperationException($"SDL.VulkanCreateSurface failed: {SDL.GetError()}");
            return new VkSurfaceKHR(surface);
        }
        // (window, instance) -> ulong
        var m4 = sdlType.GetMethod("VulkanCreateSurface", new[] { typeof(nint), typeof(nint) });
        if (m4 != null)
        {
            var surface = (ulong)(m4.Invoke(null, new object[] { _sdl.Window, (nint)instance.Handle }) ?? 0UL);
            if (surface == 0)
                throw new InvalidOperationException($"SDL.VulkanCreateSurface failed: {SDL.GetError()}");
            return new VkSurfaceKHR(surface);
        }
        throw new MissingMethodException("SDL.VulkanCreateSurface signature not found.");
    }

    public (uint Width, uint Height) GetDrawableSize()
    {
        uint w = (uint)Math.Max(1, _sdl.Width);
        uint h = (uint)Math.Max(1, _sdl.Height);
        return (w, h);
    }
}
