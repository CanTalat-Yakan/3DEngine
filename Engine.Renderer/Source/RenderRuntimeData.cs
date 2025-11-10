using System.Numerics;

namespace Engine;

public readonly struct RenderCamera
{
    public readonly Matrix4x4 View;
    public readonly Matrix4x4 Projection;
    public RenderCamera(Matrix4x4 view, Matrix4x4 projection)
    { View = view; Projection = projection; }
}

public sealed class RenderCameras
{
    public List<RenderCamera> Items { get; } = new();
}

public readonly struct DrawCommand
{
    public readonly int EntityId;
    public readonly int SortKey;
    public DrawCommand(int entityId, int sortKey)
    { EntityId = entityId; SortKey = sortKey; }
}

public sealed class RenderDrawLists
{
    public List<DrawCommand> Opaque { get; } = new();
    public List<DrawCommand> Transparent { get; } = new();
    public void Clear()
    { Opaque.Clear(); Transparent.Clear(); }
}

