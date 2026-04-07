namespace Engine;

/// <summary>Uploads per-frame camera uniform buffer and descriptor set for rendering.
/// Runs as a prepare system (after BeginFrame / fence wait) so the dynamic allocator
/// is safe to use - no per-frame buffer aliasing.</summary>
public sealed class SamplePrepare : IPrepareSystem, IDisposable
{
    private IDescriptorSet? _cameraSet;

    public void Run(RenderWorld renderWorld, RendererContext ctx, CommandRecordingContext cmds)
    {
        var cameras = renderWorld.TryGet<RenderCameras>();
        if (cameras is null || cameras.Items.Count == 0)
            return;

        var allocator = cmds.DynamicAllocator;
        if (allocator is null)
            return;

        var cam = cameras.Items[0];
        var uniform = new CameraUniform
        {
            View = cam.View,
            Projection = cam.Projection
        };

        var sizeBytes = (ulong)System.Runtime.InteropServices.Marshal.SizeOf<CameraUniform>();
        var alloc = allocator.Allocate(sizeBytes, BufferUsage.Uniform);

        var span = allocator.Map(alloc);
        System.Runtime.InteropServices.MemoryMarshal.Write(span, in uniform);
        allocator.Unmap(alloc);

        if (_cameraSet is null)
        {
            _cameraSet = ctx.Graphics.CreateDescriptorSet();
        }

        var uboBinding = new UniformBufferBinding(alloc.Buffer, 0, alloc.Offset, sizeBytes);
        ctx.Graphics.UpdateDescriptorSet(_cameraSet, uboBinding, samplerBinding: null);

        renderWorld.Set(_cameraSet);
    }

    public void Dispose()
    {
        _cameraSet?.Dispose();
    }
}
