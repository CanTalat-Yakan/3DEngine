namespace Engine;

public sealed class SamplePrepare : IPrepareSystem
{
    public void Run(RenderWorld renderWorld, RendererContext ctx)
    {
        // TODO: Upload/allocate GPU buffers & descriptor sets once Vulkan backend is implemented.
    }
}
