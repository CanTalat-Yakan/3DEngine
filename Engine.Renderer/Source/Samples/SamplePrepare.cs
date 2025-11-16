namespace Engine;

public sealed class SamplePrepare : IPrepareSystem
{
    private IBuffer? _cameraBuffer;
    private IDescriptorSet? _cameraSet;

    public void Run(RenderWorld renderWorld, RendererContext ctx)
    {
        // Look up the first render camera, if any.
        var cameras = renderWorld.TryGet<RenderCameras>();
        if (cameras is null || cameras.Items.Count == 0)
            return;

        var cam = cameras.Items[0];
        var uniform = new CameraUniform
        {
            View = cam.View,
            Projection = cam.Projection
        };

        // Lazily allocate a uniform buffer and descriptor set once.
        if (_cameraBuffer is null)
        {
            var size = (ulong)System.Runtime.InteropServices.Marshal.SizeOf<CameraUniform>();
            var desc = new BufferDesc(size, BufferUsage.Uniform | BufferUsage.TransferDst, CpuAccessMode.Write);
            _cameraBuffer = ctx.Graphics.CreateBuffer(desc);
        }

        if (_cameraSet is null)
        {
            _cameraSet = ctx.Graphics.CreateDescriptorSet();
        }

        // Upload camera data into the uniform buffer.
        var span = ctx.Graphics.Map(_cameraBuffer);
        System.Runtime.InteropServices.MemoryMarshal.Write(span, ref uniform);
        ctx.Graphics.Unmap(_cameraBuffer);

        // Bind the camera UBO at binding 0 on the descriptor set.
        var sizeBytes = (ulong)System.Runtime.InteropServices.Marshal.SizeOf<CameraUniform>();
        var uboBinding = new UniformBufferBinding(_cameraBuffer, 0, 0, sizeBytes);
        ctx.Graphics.UpdateDescriptorSet(_cameraSet, uboBinding, samplerBinding: null);

        // Store the descriptor set in the render world for queue systems / nodes to consume.
        renderWorld.Set(_cameraSet);
    }
}
