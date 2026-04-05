namespace Engine;

/// <summary>
/// Extracts data from the world into the render world.
/// Runs before <c>BeginFrame</c>.
/// </summary>
public interface IExtractSystem
{
    void Run(World world, RenderWorld renderWorld);
}
