namespace Engine;

/// <summary>
/// A single draw command representing one renderable entity in a draw list.
/// Sorted by <see cref="SortKey"/> for front-to-back (opaque) or back-to-front (transparent) rendering.
/// </summary>
/// <seealso cref="RenderDrawLists"/>
public readonly struct DrawCommand
{
    /// <summary>The entity ID of the renderable object.</summary>
    public readonly int EntityId;

    /// <summary>Sort key for depth ordering (lower values render first in opaque lists).</summary>
    public readonly int SortKey;

    /// <summary>Creates a new draw command for the specified entity with the given sort key.</summary>
    /// <param name="entityId">The entity ID.</param>
    /// <param name="sortKey">The depth sort key.</param>
    public DrawCommand(int entityId, int sortKey)
    {
        EntityId = entityId;
        SortKey = sortKey;
    }
}