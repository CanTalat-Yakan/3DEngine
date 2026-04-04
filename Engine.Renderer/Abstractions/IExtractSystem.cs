namespace Engine;

/// <summary>
/// Extracts data from the app world into the render world.
/// Runs before <c>BeginFrame</c>.
/// </summary>
public interface IExtractSystem
{
    void Run(object appWorld, RenderWorld renderWorld);
}
