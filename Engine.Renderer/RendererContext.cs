using System.Diagnostics;

namespace Engine;

/// <summary>
/// High-level facade over the low-level Vulkan graphics backend.
/// Manages the graphics device lifecycle, camera uniform buffers, descriptor sets,
/// and the <see cref="DynamicBufferAllocator"/> for transient GPU allocations.
/// </summary>
/// <seealso cref="Renderer"/>
/// <seealso cref="IGraphicsDevice"/>
/// <seealso cref="DynamicBufferAllocator"/>
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

    /// <summary>The low-level graphics device for GPU resource creation and rendering commands.</summary>
    /// <exception cref="InvalidOperationException">Accessed before <see cref="Initialize"/> has been called.</exception>
    public IGraphicsDevice Graphics => _graphics ?? throw new InvalidOperationException("Graphics device not created. Call Initialize first.");

    /// <summary>Creates a new <see cref="RendererContext"/> using the default <see cref="GraphicsDevice"/> factory.</summary>
    public RendererContext()
    {
        _graphicsFactory = static () => new GraphicsDevice();
    }

    /// <summary>Creates a new <see cref="RendererContext"/> with a pre-existing graphics device.</summary>
    /// <param name="graphics">The graphics device to use.</param>
    public RendererContext(IGraphicsDevice graphics)
    {
        _graphics = graphics;
    }

    /// <summary>Creates a new <see cref="RendererContext"/> with a custom graphics device factory.</summary>
    /// <param name="graphicsFactory">Factory delegate invoked once during <see cref="Initialize"/>.</param>
    public RendererContext(Func<IGraphicsDevice> graphicsFactory)
    {
        _graphicsFactory = graphicsFactory;
    }

    /// <summary>Whether the underlying graphics device has been initialized.</summary>
    public bool IsInitialized => _graphics?.IsInitialized ?? false;

    /// <summary>Information about the currently selected graphics adapter (GPU).</summary>
    /// <exception cref="InvalidOperationException">Accessed before <see cref="Initialize"/> has been called.</exception>
    public GraphicsAdapterInfo AdapterInfo => _graphics?.AdapterInfo ?? throw new InvalidOperationException("RendererContext not initialized.");

    /// <summary>Initializes the graphics device, creates per-frame camera resources, and sets up the dynamic buffer allocator.</summary>
    /// <param name="surfaceSource">Platform surface source for creating the swapchain.</param>
    /// <param name="appName">Application name embedded in the Vulkan instance.</param>
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

    /// <summary>Creates per-swapchain-image camera uniform buffers and descriptor sets.</summary>
    private void CreateCameraResources()
    {
        DisposeCameraResources();

        if (!IsInitialized)
            return;

        var imageCount = (int)_graphics!.Swapchain.ImageCount;
        if (imageCount <= 0)
            return;

        var newBuffers = new IBuffer[imageCount];
        var newSets = new IDescriptorSet[imageCount];

        ulong cameraSize = (ulong)System.Runtime.InteropServices.Marshal.SizeOf<CameraUniform>();
        try
        {
            for (int i = 0; i < imageCount; i++)
            {
                var desc = new BufferDesc(cameraSize, BufferUsage.Uniform | BufferUsage.TransferDst, CpuAccessMode.Write);
                newBuffers[i] = _graphics!.CreateBuffer(desc);
                newSets[i] = _graphics.CreateDescriptorSet();

                var ubBinding = new UniformBufferBinding(newBuffers[i], 0, 0, cameraSize);
                _graphics.UpdateDescriptorSet(newSets[i], ubBinding, samplerBinding: null);
            }
        }
        catch (Exception ex)
        {
            Logger.Error("Failed to create camera resources - cleaning up partial state.", ex);
            for (int i = 0; i < imageCount; i++)
            {
                newSets[i]?.Dispose();
                newBuffers[i]?.Dispose();
            }
            return;
        }

        _cameraBuffers = newBuffers;
        _cameraDescriptorSets = newSets;
    }

    /// <summary>Disposes camera uniform buffers and descriptor sets.</summary>
    private void DisposeCameraResources()
    {
        if (_cameraBuffers != null)
        {
            foreach (var buf in _cameraBuffers)
                buf?.Dispose();
            _cameraBuffers = null;
        }
        if (_cameraDescriptorSets != null)
        {
            foreach (var set in _cameraDescriptorSets)
                set?.Dispose();
            _cameraDescriptorSets = null;
        }
    }

    /// <summary>Begins a new render frame: acquires a swapchain image, uploads camera data, and returns a command recording context.</summary>
    /// <param name="world">The render world containing camera and clear color data.</param>
    /// <param name="imageIndex">The acquired swapchain image index.</param>
    /// <returns>A <see cref="CommandRecordingContext"/> for recording GPU commands this frame.</returns>
    public CommandRecordingContext BeginFrame(RenderWorld world, out uint imageIndex)
    {
        if (!IsInitialized) throw new InvalidOperationException("RendererContext not initialized.");

        var clear = world.TryGet<ClearColor>() is { } cc
            ? cc
            : ClearColor.Black;

        var frame = _graphics!.BeginFrame(clear);
        imageIndex = frame.FrameIndex;

        // Advance the dynamic allocator - the fence wait inside BeginFrame guarantees
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

    /// <summary>Uploads a camera uniform struct to the specified buffer via map/write/unmap.</summary>
    /// <param name="buffer">The target uniform buffer.</param>
    /// <param name="camera">The camera uniform data to upload.</param>
    private void UploadCameraUniform(IBuffer buffer, in CameraUniform camera)
    {
        var span = _graphics!.Map(buffer);
        System.Runtime.InteropServices.MemoryMarshal.Write(span, in camera);
        _graphics.Unmap(buffer);
    }

    /// <summary>Begins a new render frame without returning the image index.</summary>
    /// <param name="world">The render world containing camera and clear color data.</param>
    /// <returns>A <see cref="CommandRecordingContext"/> for recording GPU commands this frame.</returns>
    public CommandRecordingContext BeginFrame(RenderWorld world) => BeginFrame(world, out _);

    /// <summary>Ends the current frame and submits it for presentation.</summary>
    /// <param name="ctx">The command recording context for this frame.</param>
    /// <param name="imageIndex">The swapchain image index to present.</param>
    public void EndFrame(CommandRecordingContext ctx, uint imageIndex)
    {
        _graphics!.EndFrame(ctx.FrameContext);
        ctx.Dispose();
    }

    /// <summary>Ends the current frame using the image index from the frame context.</summary>
    /// <param name="ctx">The command recording context for this frame.</param>
    public void EndFrame(CommandRecordingContext ctx) => EndFrame(ctx, ctx.FrameContext.FrameIndex);

    /// <summary>Handles a resize event by recreating the swapchain, dynamic allocator, and camera resources.</summary>
    public void OnResize()
    {
        if (!IsInitialized)
            return;

        Logger.Info("RendererContext: Resize triggered - recreating swapchain and camera resources...");
        try
        {
            _graphics!.OnResize();
        }
        catch (Exception ex)
        {
            Logger.Error("RendererContext: Swapchain resize failed - cleaning up resources.", ex);
            DisposeCameraResources();
            DynamicAllocator?.Reset();
            return;
        }

        DynamicAllocator?.Reset();
        CreateCameraResources();
        Logger.Info("RendererContext: Resize complete.");
    }

    /// <inheritdoc />
    public void Dispose()
    {
        Logger.Info("RendererContext: Disposing camera resources, dynamic allocator, and graphics device...");
        DynamicAllocator?.Dispose();
        DynamicAllocator = null;
        DisposeCameraResources();

        _graphics?.Dispose();
        Logger.Info("RendererContext disposed.");
    }
}