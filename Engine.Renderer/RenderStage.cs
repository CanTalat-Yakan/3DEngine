namespace Engine;

/// <summary>Phases of the per-frame render pipeline.</summary>
/// <seealso cref="Renderer"/>
public enum RenderStage
{
    /// <summary>Copies game-world data into the <see cref="RenderWorld"/>.</summary>
    Extract,
    /// <summary>Uploads GPU resources (buffers, textures) from extracted data.</summary>
    Prepare,
    /// <summary>Builds draw command lists from prepared data.</summary>
    Queue,
    /// <summary>Executes render graph nodes, issuing GPU draw calls.</summary>
    Execute
}