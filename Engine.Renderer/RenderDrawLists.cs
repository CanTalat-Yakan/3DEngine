namespace Engine;

/// <summary>
/// Sorted draw command lists for opaque and transparent geometry.
/// Cleared each frame before the queue phase populates them.
/// </summary>
/// <seealso cref="Renderer"/>
public sealed class RenderDrawLists
{
    /// <summary>Opaque draw commands (rendered front-to-back).</summary>
    public List<DrawCommand> Opaque { get; } = new();

    /// <summary>Transparent draw commands (rendered back-to-front).</summary>
    public List<DrawCommand> Transparent { get; } = new();

    /// <summary>Clears both opaque and transparent lists.</summary>
    public void Clear()
    {
        Opaque.Clear();
        Transparent.Clear();
    }
}