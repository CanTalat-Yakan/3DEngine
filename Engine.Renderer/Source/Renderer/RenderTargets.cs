using System;

namespace Engine;

/// <summary>Describes the size of the primary presentation surface (e.g., swapchain/backbuffer).</summary>
public sealed class RenderSurfaceInfo
{
    public int Width { get; set; }
    public int Height { get; set; }
    public ulong Revision { get; private set; }

    public bool Apply(int width, int height)
    {
        var clampedWidth = Math.Max(1, width);
        var clampedHeight = Math.Max(1, height);
        if (Width == clampedWidth && Height == clampedHeight)
            return false;
        Width = clampedWidth;
        Height = clampedHeight;
        Revision++;
        return true;
    }
}

/// <summary>Lightweight description of a render texture target.</summary>
public readonly struct RenderTextureDesc
{
    public readonly int Width;
    public readonly int Height;
    public RenderTextureDesc(int width, int height)
    { Width = width; Height = height; }
}

/// <summary>Registry of named render textures available for cameras to target.</summary>
public sealed class RenderTextures
{
    private readonly Dictionary<string, RenderTextureDesc> _textures = new();

    public void Set(string name, RenderTextureDesc desc) => _textures[name] = desc;
    public bool TryGet(string name, out RenderTextureDesc desc) => _textures.TryGetValue(name, out desc);
    public void Remove(string name) => _textures.Remove(name);
}
