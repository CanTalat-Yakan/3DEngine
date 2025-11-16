namespace Engine;

public sealed class Pipeline : IPipeline
{
    public void Dispose()
    {
        // Legacy placeholder; real Vulkan-backed pipelines are implemented
        // in GraphicsDevice.Pipeline.cs via IPipeline.
    }
}
