namespace Engine;

// High-level facade over the low-level graphics backend (e.g., Vulkan).
public sealed class RendererContext : IDisposable
{
    private static readonly ILogger Logger = Log.For<RendererContext>();
    private IGraphicsDevice? _graphics;
    private readonly Func<IGraphicsDevice>? _graphicsFactory;

    // Per-frame camera uniform buffers and descriptor sets, sized to the swapchain image count.
    private IBuffer[]? _cameraBuffers;
    private IDescriptorSet[]? _cameraDescriptorSets;

    // Expose the underlying graphics device for internal systems that need advanced access.
    public IGraphicsDevice Graphics => _graphics ?? throw new InvalidOperationException("Graphics device not created. Call Initialize first.");

    // Default constructor defers backend creation until Initialize to avoid early assembly loads.
    public RendererContext()
    {
        _graphicsFactory = static () => new GraphicsDevice();
    }

    public RendererContext(IGraphicsDevice graphics)
    {
        _graphics = graphics;
    }

    public RendererContext(Func<IGraphicsDevice> graphicsFactory)
    {
        _graphicsFactory = graphicsFactory;
    }

    public bool IsInitialized => _graphics?.IsInitialized ?? false;

    public GraphicsAdapterInfo AdapterInfo => _graphics?.AdapterInfo ?? throw new InvalidOperationException("RendererContext not initialized.");

    public void Initialize(ISurfaceSource surfaceSource, string appName = "3DEngine")
    {
        if (IsInitialized) return;

        // Lazily create the backend if not supplied via ctor.
        _graphics ??= _graphicsFactory?.Invoke() ?? new GraphicsDevice();

        _graphics.Initialize(surfaceSource, appName);

        CreateCameraResources();
    }

    private void CreateCameraResources()
    {
        // Dispose old resources if present.
        if (_cameraBuffers != null)
        {
            foreach (var buf in _cameraBuffers)
                buf.Dispose();
        }
        if (_cameraDescriptorSets != null)
        {
            foreach (var set in _cameraDescriptorSets)
                set.Dispose();
        }

        if (!IsInitialized)
        {
            _cameraBuffers = null;
            _cameraDescriptorSets = null;
            return;
        }

        var imageCount = (int)_graphics!.Swapchain.ImageCount;
        if (imageCount <= 0)
        {
            _cameraBuffers = null;
            _cameraDescriptorSets = null;
            return;
        }

        _cameraBuffers = new IBuffer[imageCount];
        _cameraDescriptorSets = new IDescriptorSet[imageCount];

        ulong cameraSize = (ulong)System.Runtime.InteropServices.Marshal.SizeOf<CameraUniform>();
        for (int i = 0; i < imageCount; i++)
        {
            var desc = new BufferDesc(cameraSize, BufferUsage.Uniform | BufferUsage.TransferDst);
            var buffer = _graphics!.CreateBuffer(desc);
            _cameraBuffers[i] = buffer;

            var set = _graphics.CreateDescriptorSet();
            _cameraDescriptorSets[i] = set;

            // UniformBufferBinding(IBuffer Buffer, uint Binding, ulong Offset, ulong Size)
            var ubBinding = new UniformBufferBinding(buffer, 0, 0, cameraSize);
            _graphics.UpdateDescriptorSet(set, ubBinding, samplerBinding: null);
        }
    }

    public CommandRecordingContext BeginFrame(RenderWorld world, out uint imageIndex)
    {
        if (!IsInitialized) throw new InvalidOperationException("RendererContext not initialized.");

        var clear = world.TryGet<RenderClearColor>() is { } cc
            ? new ClearColor(cc.R, cc.G, cc.B, cc.A)
            : ClearColor.Black;

        var frame = _graphics!.BeginFrame(clear);
        imageIndex = frame.FrameIndex; // treat frame index as swapchain image index

        // Update per-frame camera UBO for the current image index, if available.
        if (_cameraBuffers is { Length: > 0 } cameraBuffers && _cameraDescriptorSets is { Length: > 0 })
        {
            var cameras = world.TryGet<RenderCameras>();
            if (cameras is { Items.Count: > 0 })
            {
                var cam = cameras.Items[0];
                var cameraUniform = new CameraUniform
                {
                    View = cam.View,
                    Projection = cam.Projection
                };

                int idx = (int)(imageIndex % (uint)cameraBuffers.Length);
                var buffer = cameraBuffers[idx];

                UploadCameraUniform(buffer, cameraUniform);
            }
        }

        return new CommandRecordingContext(frame);
    }

    private void UploadCameraUniform(IBuffer buffer, in CameraUniform camera)
    {
        var span = _graphics!.Map(buffer);
        System.Runtime.InteropServices.MemoryMarshal.Write(span, in camera);
        _graphics.Unmap(buffer);
    }

    public CommandRecordingContext BeginFrame(RenderWorld world) => BeginFrame(world, out _);

    public void EndFrame(CommandRecordingContext ctx, uint imageIndex)
    {
        _graphics!.EndFrame(ctx.FrameContext);
        ctx.Dispose();
    }

    public void EndFrame(CommandRecordingContext ctx) => EndFrame(ctx, ctx.FrameContext.FrameIndex);

    public void OnResize()
    {
        if (!IsInitialized)
            return;

        _graphics!.OnResize();
        CreateCameraResources();
    }

    public void Dispose()
    {
        if (_cameraBuffers != null)
        {
            foreach (var buf in _cameraBuffers)
                buf.Dispose();
            _cameraBuffers = null;
        }
        if (_cameraDescriptorSets != null)
        {
            foreach (var set in _cameraDescriptorSets)
                set.Dispose();
            _cameraDescriptorSets = null;
        }

        _graphics?.Dispose();
    }
}