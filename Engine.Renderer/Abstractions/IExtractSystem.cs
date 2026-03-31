namespace Engine;

/// <summary>Extracts data from the app world into the render world.</summary>
public interface IExtractSystem
{
    void Run(object appWorld, RenderWorld renderWorld);
}
