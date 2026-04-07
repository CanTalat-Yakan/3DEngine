namespace Engine;

/// <summary>Describes the size of the primary presentation surface (e.g., swapchain/backbuffer).</summary>
/// <seealso cref="Renderer"/>
public sealed class RenderSurfaceInfo
{
    /// <summary>Current surface width in pixels (≥ 1).</summary>
    public int Width { get; set; }

    /// <summary>Current surface height in pixels (≥ 1).</summary>
    public int Height { get; set; }

    /// <summary>Monotonically increasing revision counter, bumped on each resize.</summary>
    public ulong Revision { get; private set; }

    /// <summary>Updates the surface dimensions. Returns <c>true</c> if the size changed.</summary>
    /// <param name="width">New width in pixels (clamped to ≥ 1).</param>
    /// <param name="height">New height in pixels (clamped to ≥ 1).</param>
    /// <returns><c>true</c> if the dimensions changed; <c>false</c> if they were already the same.</returns>
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
/// <seealso cref="RenderTextures"/>
public readonly struct RenderTextureDesc
{
    /// <summary>Texture width in pixels.</summary>
    public readonly int Width;

    /// <summary>Texture height in pixels.</summary>
    public readonly int Height;

    /// <summary>Creates a new <see cref="RenderTextureDesc"/> with the given dimensions.</summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    public RenderTextureDesc(int width, int height)
    { Width = width; Height = height; }
}

/// <summary>Registry of named render textures available for cameras to target.</summary>
/// <seealso cref="RenderTextureDesc"/>
public sealed class RenderTextures
{
    private readonly Dictionary<string, RenderTextureDesc> _textures = new();

    /// <summary>Sets (inserts or overwrites) a named render texture.</summary>
    /// <param name="name">Unique name for the render texture.</param>
    /// <param name="desc">The texture descriptor.</param>
    public void Set(string name, RenderTextureDesc desc) => _textures[name] = desc;

    /// <summary>Tries to get the descriptor for a named render texture.</summary>
    /// <param name="name">Name of the render texture.</param>
    /// <param name="desc">When returning <c>true</c>, contains the descriptor; otherwise <c>default</c>.</param>
    /// <returns><c>true</c> if found; otherwise <c>false</c>.</returns>
    public bool TryGet(string name, out RenderTextureDesc desc) => _textures.TryGetValue(name, out desc);

    /// <summary>Removes a named render texture.</summary>
    /// <param name="name">Name of the render texture to remove.</param>
    public void Remove(string name) => _textures.Remove(name);
}
