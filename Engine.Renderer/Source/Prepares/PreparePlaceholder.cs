namespace Engine;

public sealed class PreparePlaceholder : IPrepareSystem
{
    public void Run(Engine.RenderWorld renderWorld, RendererContext ctx)
    {
        // TODO: Upload/allocate GPU buffers & descriptor sets once Vulkan backend is implemented.
    }
}
