namespace Engine;

/// <summary>Prepares render resources using extracted data.</summary>
public interface IPrepareSystem
{
    void Run(RenderWorld renderWorld, RendererContext ctx);
}

