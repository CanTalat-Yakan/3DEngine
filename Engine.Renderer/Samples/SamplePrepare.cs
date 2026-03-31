namespace Engine;

/// <summary>Prepares per-frame camera uniform buffer and descriptor set for rendering.</summary>
public sealed class SamplePrepare : IPrepareSystem, IDisposable
{
    private IBuffer? _cameraBuffer;
    private IDescriptorSet? _cameraSet;

    public void Run(RenderWorld renderWorld, RendererContext ctx)
    {
        var cameras = renderWorld.TryGet<RenderCameras>();
        if (cameras is null || cameras.Items.Count == 0)
            return;

        var cam = cameras.Items[0];
        var uniform = new CameraUniform
        {
            View = cam.View,
            Projection = cam.Projection
        };

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

        var span = ctx.Graphics.Map(_cameraBuffer);
        System.Runtime.InteropServices.MemoryMarshal.Write(span, in uniform);
        ctx.Graphics.Unmap(_cameraBuffer);

        var sizeBytes = (ulong)System.Runtime.InteropServices.Marshal.SizeOf<CameraUniform>();
        var uboBinding = new UniformBufferBinding(_cameraBuffer, 0, 0, sizeBytes);
        ctx.Graphics.UpdateDescriptorSet(_cameraSet, uboBinding, samplerBinding: null);

        renderWorld.Set(_cameraSet);
    }

    public void Dispose()
    {
        _cameraSet?.Dispose();
        _cameraBuffer?.Dispose();
    }
}
