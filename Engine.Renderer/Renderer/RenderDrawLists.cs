namespace Engine;

public sealed class RenderDrawLists
{
    public List<DrawCommand> Opaque { get; } = new();
    public List<DrawCommand> Transparent { get; } = new();
    public void Clear()
    { Opaque.Clear(); Transparent.Clear(); }
}