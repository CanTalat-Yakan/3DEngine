using System.Diagnostics;

namespace Engine;

/// <summary>High-level facade over the low-level vulkan graphics backend.</summary>
public sealed class RendererContext : IDisposable
{
    private static readonly ILogger Logger = Log.For<RendererContext>();
    private IGraphicsDevice? _graphics;
    private readonly Func<IGraphicsDevice>? _graphicsFactory;

    private IBuffer[]? _cameraBuffers;
    private IDescriptorSet[]? _cameraDescriptorSets;

    /// <summary>Frame-aware dynamic buffer allocator for transient GPU data.
    /// Available after <see cref="Initialize"/> has been called.</summary>
    public DynamicBufferAllocator? DynamicAllocator { get; private set; }

    public IGraphicsDevice Graphics => _graphics ?? throw new InvalidOperationException("Graphics device not created. Call Initialize first.");

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

        Logger.Info("RendererContext: Initializing graphics backend...");
        var sw = Stopwatch.StartNew();

        _graphics ??= _graphicsFactory?.Invoke() ?? new GraphicsDevice();
        Logger.Debug($"Graphics device type: {_graphics.GetType().Name}");

        _graphics.Initialize(surfaceSource, appName);

        Logger.Debug("Creating per-swapchain-image camera uniform buffers and descriptor sets...");
        CreateCameraResources();

        Logger.Debug("Creating dynamic buffer allocator...");
        DynamicAllocator = new DynamicBufferAllocator(_graphics);

        Logger.Info($"RendererContext initialized in {sw.ElapsedMilliseconds}ms.");
    }

    private void CreateCameraResources()
    {
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

            var ubBinding = new UniformBufferBinding(buffer, 0, 0, cameraSize);
            _graphics.UpdateDescriptorSet(set, ubBinding, samplerBinding: null);
        }
    }

    public CommandRecordingContext BeginFrame(RenderWorld world, out uint imageIndex)
    {
        if (!IsInitialized) throw new InvalidOperationException("RendererContext not initialized.");

        var clear = world.TryGet<ClearColor>() is { } cc
            ? cc
            : ClearColor.Black;

        var frame = _graphics!.BeginFrame(clear);
        imageIndex = frame.FrameIndex;

        // Advance the dynamic allocator — the fence wait inside BeginFrame guarantees
        // this in-flight slot is idle, so resetting its cursors is safe.
        DynamicAllocator?.BeginFrame(frame.InFlightIndex);

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

        return new CommandRecordingContext(frame, DynamicAllocator);
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

        Logger.Info("RendererContext: Resize triggered — recreating swapchain and camera resources...");
        _graphics!.OnResize();
        DynamicAllocator?.Reset();
        CreateCameraResources();
        Logger.Info("RendererContext: Resize complete.");
    }

    public void Dispose()
    {
        Logger.Info("RendererContext: Disposing camera resources, dynamic allocator, and graphics device...");
        DynamicAllocator?.Dispose();
        DynamicAllocator = null;
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
        Logger.Info("RendererContext disposed.");
    }
}