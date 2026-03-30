namespace Engine;

public readonly struct DrawCommand
{
    public readonly int EntityId;
    public readonly int SortKey;

    public DrawCommand(int entityId, int sortKey)
    {
        EntityId = entityId;
        SortKey = sortKey;
    }
}