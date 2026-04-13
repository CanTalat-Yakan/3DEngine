using UltralightNet;
using UltralightNet.Enums;
using UltralightNet.Structs;
using UltralightNet.Platform;

using ULRenderer = UltralightNet.Renderer;
using ULPlatform = UltralightNet.Platform.Platform;

namespace Engine;

/// <summary>
/// Manages the Ultralight renderer and view lifecycle.
/// Stored as a <see cref="World"/> resource.
/// </summary>
/// <remarks>
/// <para>
/// Wraps the Ultralight <see cref="UltralightNet.Renderer"/> and <see cref="View"/>
/// objects, providing CPU-mode bitmap surface rendering suitable for texture upload to
/// a GPU pipeline.  The view is repainted every frame via forced <c>NeedsPaint</c>,
/// ensuring scroll, CSS animation, hover, and other visual changes are always reflected.
/// </para>
/// <para>
/// The class implements a <em>quiescent resize</em> pattern: when the view is resized
/// via <see cref="Resize"/>, the actual native resize is deferred to the next
/// <see cref="Update"/> call, and on that frame all Ultralight work is skipped so the
/// freshly-reallocated surface is not accessed before being painted into.
/// </para>
/// <para>Thread safety: this class is <b>not</b> thread-safe. All calls must occur on the
/// same thread (typically the main thread via <c>MainThreadOnly()</c> system descriptors).</para>
/// </remarks>
/// <seealso cref="WebViewPlugin"/>
/// <seealso cref="WebViewRenderNode"/>
/// <seealso cref="WebViewInput"/>
public sealed class WebViewInstance : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.WebView");

    private ULRenderer? _renderer;
    private View? _view;
    private bool _disposed;

    /// <summary>Whether the webview overlay is rendered. Toggled at runtime via F1 (<see cref="BehaviorConditions.KeyToggle"/>).</summary>
    public bool Visible { get; set; } = true;

    /// <summary>Current pixel width of the webview view.</summary>
    public uint Width { get; private set; }

    /// <summary>Current pixel height of the webview view.</summary>
    public uint Height { get; private set; }

    /// <summary>The underlying Ultralight view (null before <see cref="Initialize"/>).</summary>
    public View? View => _view;

    /// <summary>Row stride in bytes of the bitmap surface (may be &gt; Width*4 due to alignment).</summary>
    public uint SurfaceRowBytes => _view?.Surface?.RowBytes ?? (Width * 4);

    // ── Diagnostics (read by ImGui debug window) ─────────────────────

    /// <summary>Whether the Ultralight view has an active bitmap surface.</summary>
    public bool HasSurface => _view?.Surface is not null;

    /// <summary>Total number of <see cref="Update"/> invocations since initialization.</summary>
    public int DiagUpdateCount { get; private set; }

    /// <summary>Total number of frames where Ultralight itself reported a dirty region (before we force-repaint).</summary>
    public int DiagPaintCount { get; private set; }

    /// <summary>Total number of pixel uploads to the GPU texture (incremented by the render node).</summary>
    public int DiagUploadCount { get; internal set; }

    /// <summary>Total pixel count (<c>Width × Height</c>) for the current view dimensions.</summary>
    public long DiagTotalPixels => Width * Height;

    /// <summary>Page title from the native View.Title property.</summary>
    public string? DiagPageTitle { get; private set; }

    /// <summary>Whether the ICU data file exists on disk.</summary>
    public bool DiagIcuExists => File.Exists(Path.Combine(AppContext.BaseDirectory, "runtimes", "icudt67l.dat"));

    /// <summary>Whether the CA certificate file exists on disk.</summary>
    public bool DiagCaCertExists => File.Exists(Path.Combine(AppContext.BaseDirectory, "runtimes", "cacert.pem"));

    // ── Page load state (set by native callbacks) ────────────────────

    /// <summary>Whether the DOM-ready callback has fired for the current page.</summary>
    public bool DiagDOMReady { get; private set; }

    /// <summary>Whether the page-finished-loading callback has fired for the current page.</summary>
    public bool DiagPageFinished { get; private set; }

    /// <summary>Formatted error string from the last page-load failure, or <c>null</c> if no error.</summary>
    public string? DiagLoadError { get; private set; }

    /// <summary>Last JavaScript console message captured by the native callback.</summary>
    public string? DiagLastConsoleMessage { get; private set; }

    private int _titleQueryCountdown;

    // ── Deferred resize ─────────────────────────────────────────────
    private uint _pendingResizeWidth;
    private uint _pendingResizeHeight;
    private bool _hasPendingResize;

    /// <summary>
    /// Monotonically-increasing generation counter, bumped on every committed
    /// native resize.  The render node tracks this to skip pixel reads on the
    /// frame where a resize occurred (the surface has been reallocated and no
    /// Ultralight Render() has painted into it yet).
    /// </summary>
    public uint ResizeGeneration { get; private set; }

    /// <summary>
    /// Initializes the Ultralight platform, renderer, and view.
    /// Call once during Startup.
    /// </summary>
    /// <param name="width">Initial pixel width of the Ultralight view.</param>
    /// <param name="height">Initial pixel height of the Ultralight view.</param>
    public void Initialize(uint width, uint height)
    {
        Logger.Info($"WebViewInstance: Initializing Ultralight ({width}x{height}, CPU bitmap mode)...");
        Width = width;
        Height = height;

        // ── Extract embedded resources to disk ────────────────────────
        ExtractEmbeddedResources();

        // ── Platform configuration via AppCore ────────────────────────
        AppCoreMethods.SetDefaultLogger(Path.Combine(AppContext.BaseDirectory, "ultralight.log"));
        AppCoreMethods.SetPlatformFontLoader();
        AppCoreMethods.SetPlatformFileSystem(AppContext.BaseDirectory);

        ULPlatform.ErrorGpuDriverNotSet = false;

        Logger.Info("AppCore platform font loader, file system, and logger enabled.");

        // ── Renderer ──────────────────────────────────────────────────
        _renderer = ULPlatform.CreateRenderer();

        // CreateRenderer() may reset ErrorWrongThread to true internally.
        // The engine's Update stage runs systems in parallel (thread pool),
        // so we must disable the managed thread-affinity check afterwards.
        ULPlatform.ErrorWrongThread = false;
        Logger.Info("Ultralight renderer created.");

        // ── View configuration ────────────────────────────────────────
        var viewConfig = new ViewConfig
        {
            IsAccelerated = false,   // CPU bitmap surface mode
            IsTransparent = true,    // transparent background for compositing
            InitialDeviceScale = 1.0,
            InitialFocus = true,
            EnableImages = true,
            EnableJavaScript = true,
            FontFamilyStandard = "Arial",
            FontFamilyFixed = "Courier New",
            FontFamilySerif = "Times New Roman",
            FontFamilySansSerif = "Arial",
            UserAgent = "Mozilla/5.0 (3DEngine WebView) UltralightNet/1.4",
        };

        _view = _renderer.CreateView(width, height, viewConfig, _renderer.DefaultSession);
        _view.Focus();

        // ── Register native callbacks for load tracking ──────────────
        _view.OnDomReady += OnDOMReadyHandler;
        _view.OnFinishLoading += OnFinishLoadingHandler;
        _view.OnFailLoading += OnFailLoadingHandler;
        _view.OnAddConsoleMessage += OnConsoleMessageHandler;

        Logger.Info($"Ultralight view created ({width}x{height}, CPU bitmap, transparent).");
    }

    // ── Native callback handlers ─────────────────────────────────────

    private void OnDOMReadyHandler(ulong frameId, bool isMainFrame, string url)
    {
        DiagDOMReady = true;
        Logger.Info($"WebViewInstance: DOM ready (frame={frameId}, main={isMainFrame}, url={url})");
    }

    private void OnFinishLoadingHandler(ulong frameId, bool isMainFrame, string url)
    {
        DiagPageFinished = true;
        Logger.Info($"WebViewInstance: Page finished loading (frame={frameId}, main={isMainFrame}, url={url})");
    }

    private void OnFailLoadingHandler(ulong frameId, bool isMainFrame, string url, string description, string errorDomain, int errorCode)
    {
        DiagLoadError = $"{errorDomain}:{errorCode} {description}";
        Logger.Warn($"WebViewInstance: Page load FAILED - {DiagLoadError} (url={url})");
    }

    private void OnConsoleMessageHandler(MessageSource source, MessageLevel level, string message, uint lineNumber, uint columnNumber, string sourceId)
    {
        DiagLastConsoleMessage = $"[{level}] {message}";
        if (level == MessageLevel.Error || level == MessageLevel.Warning)
            Logger.Warn($"WebViewInstance: Console {level}: {message} ({sourceId}:{lineNumber}:{columnNumber})");
    }

    /// <summary>Loads raw HTML content into the view.</summary>
    /// <param name="html">The HTML markup to load. Must not be <c>null</c>.</param>
    public void LoadHtml(string html)
    {
        if (_view is null) return;
        Logger.Info("WebViewInstance: Loading HTML content...");
        _view.LoadHtml(html);
        WarmUp();
    }

    /// <summary>Loads a URL into the view.</summary>
    /// <param name="url">The URL to navigate to (e.g. <c>"https://example.com"</c> or <c>"file:///..."</c>).</param>
    public void LoadUrl(string url)
    {
        if (_view is null) return;
        Logger.Info($"WebViewInstance: Loading URL: {url}");
        _view.Url = url;
        WarmUp();
    }

    /// <summary>
    /// Pumps the renderer several times to give the page a chance to parse,
    /// layout, and produce initial paint during Startup (before the main loop).
    /// </summary>
    private void WarmUp()
    {
        if (_renderer is null) return;
        for (int i = 0; i < 10; i++)
        {
            _renderer.Update();
            _renderer.Render();
        }
        Logger.Info($"WebViewInstance: Warm-up complete (DOMReady={DiagDOMReady}, Finished={DiagPageFinished}).");
    }

    /// <summary>
    /// Evaluates JavaScript in the view context.
    /// Returns the result string or null on error.
    /// </summary>
    /// <param name="js">The JavaScript source code to evaluate.</param>
    /// <returns>The string result of evaluation, or <c>null</c> if the view is uninitialized or a JS exception occurred.</returns>
    public string? EvaluateScript(string js)
    {
        if (_view is null) return null;
        var result = _view.EvaluateScript(js, out var exception);
        if (!string.IsNullOrEmpty(exception))
        {
            Logger.Warn($"JS exception: {exception}");
            return null;
        }
        return result;
    }

    /// <summary>
    /// Advances Ultralight internal timers and forces a full repaint every frame.
    /// Call once per frame (Update stage).
    /// </summary>
    public void Update()
    {
        if (_renderer is null) return;

        // ── Quiescent resize ─────────────────────────────────────────
        if (_hasPendingResize && ApplyPendingResize())
        {
            DiagUpdateCount++;
            return;
        }

        DiagUpdateCount++;

        // Process timers, JS, layout, network — may flag the view as needing paint.
        _renderer.Update();

        // Query page title periodically after Update() so the page has been processed.
        if (--_titleQueryCountdown <= 0)
        {
            _titleQueryCountdown = 60;
            try { DiagPageTitle = _view?.Title; }
            catch { DiagPageTitle = "(error)"; }
        }

        // Track how often Ultralight itself detected dirty regions.
        if (_view is not null && _view.NeedsPaint)
            DiagPaintCount++;

        // Force a full repaint every frame so scroll, CSS animations, hover
        // effects, and other visual changes are always reflected.
        if (_view is not null)
            _view.NeedsPaint = true;

        // Paint all views to their bitmap surfaces.
        _renderer.Render();
    }

    /// <summary>
    /// Copies the current bitmap surface pixels into <paramref name="destination"/>.
    /// </summary>
    /// <param name="destination">Target buffer that must be at least <c>SurfaceRowBytes × Height</c> bytes.</param>
    /// <param name="rowBytes">Receives the row stride in bytes of the copied data.</param>
    /// <returns><c>true</c> if pixels were copied; <c>false</c> if the surface is unavailable.</returns>
    public unsafe bool TryGetPixels(Span<byte> destination, out uint rowBytes)
    {
        rowBytes = 0;
        if (_view?.Surface is not { } surface)
            return false;

        var currentHeight = Height;
        rowBytes = surface.RowBytes;
        var byteCount = (int)(rowBytes * currentHeight);
        if (byteCount <= 0 || destination.Length < byteCount)
            return false;

        var ptr = surface.LockPixels();
        try
        {
            new ReadOnlySpan<byte>(ptr, byteCount).CopyTo(destination);
        }
        finally
        {
            surface.UnlockPixels();
        }
        return true;
    }

    /// <summary>
    /// Queues a resize for the webview view. The actual native resize is deferred
    /// to the next <see cref="Update"/> call so it happens on the correct thread
    /// and at a safe point in the frame lifecycle.
    /// No-op if size hasn't changed.
    /// </summary>
    /// <param name="width">New pixel width (must be &gt; 0).</param>
    /// <param name="height">New pixel height (must be &gt; 0).</param>
    public void Resize(uint width, uint height)
    {
        if (_view is null || (Width == width && Height == height)) return;
        if (width == 0 || height == 0) return;

        Logger.Info($"WebViewInstance: Queuing resize to {width}x{height} (deferred to Update)...");
        _pendingResizeWidth = width;
        _pendingResizeHeight = height;
        _hasPendingResize = true;
    }

    /// <summary>
    /// Applies a pending deferred resize.  Returns <c>true</c> if the native
    /// resize was committed (caller should skip Ultralight work this frame).
    /// </summary>
    private bool ApplyPendingResize()
    {
        if (!_hasPendingResize) return false;

        var newW = _pendingResizeWidth;
        var newH = _pendingResizeHeight;

        if (_renderer is null || _view is null || (Width == newW && Height == newH))
        {
            _hasPendingResize = false;
            return false;
        }

        _hasPendingResize = false;

        Logger.Info($"WebViewInstance: Resizing view to {newW}x{newH} (was {Width}x{Height})...");

        // Clear stale dirty-region rectangles that reference the old surface dimensions.
        _view.Surface?.ClearDirtyBounds();

        _view.Resize(newW, newH);
        Width = newW;
        Height = newH;

        // Bump the generation so the render node skips pixel reads this frame
        // (the surface was reallocated and hasn't been painted into yet).
        ResizeGeneration++;

        Logger.Info($"WebViewInstance: View resized to {newW}x{newH} (generation={ResizeGeneration}).");
        return true;
    }

    /// <summary>Fires a mouse event into the Ultralight view.</summary>
    public void FireMouseEvent(MouseEvent evt)
    {
        _view?.FireMouseEvent(evt);
    }

    /// <summary>Fires a key event into the Ultralight view.</summary>
    public void FireKeyEvent(KeyEvent evt)
    {
        _view?.FireKeyEvent(evt);
    }

    /// <summary>Fires a scroll event into the Ultralight view.</summary>
    public void FireScrollEvent(ScrollEvent evt)
    {
        _view?.FireScrollEvent(evt);
    }

    /// <summary>Releases the Ultralight view and renderer. Safe to call multiple times.</summary>
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Logger.Info("WebViewInstance: Disposing Ultralight resources...");
        _view?.Dispose();
        _view = null;
        _renderer?.Dispose();
        _renderer = null;
        Logger.Info("WebViewInstance disposed.");
    }

    /// <summary>
    /// Extracts the embedded Ultralight resources (ICU data, CA certs)
    /// from the managed assembly into a <c>resources/</c> directory on disk
    /// so the native AppCore file system can find them.
    /// </summary>
    private static void ExtractEmbeddedResources()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "runtimes");
        Directory.CreateDirectory(resourceDir);

        ExtractIfMissing(resourceDir, "icudt67l.dat", () => Resources.Icudt67Ldat);
        ExtractIfMissing(resourceDir, "cacert.pem", () => Resources.Cacertpem);
    }

    private static void ExtractIfMissing(string dir, string fileName, Func<Stream?> streamFactory)
    {
        var path = Path.Combine(dir, fileName);
        if (File.Exists(path))
        {
            Logger.Debug($"Resource already on disk: {path}");
            return;
        }

        using var stream = streamFactory();
        if (stream is not { CanRead: true })
        {
            Logger.Warn($"Embedded resource not available: {fileName}");
            return;
        }

        using var fs = File.Create(path);
        stream.CopyTo(fs);
        Logger.Info($"Extracted embedded resource: {path} ({fs.Length:N0} bytes)");
    }
}
