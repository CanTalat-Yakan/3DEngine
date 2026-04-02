using UltralightNet;
using UltralightNet.Platform;

using ULRenderer = UltralightNet.Renderer;

namespace Engine;

/// <summary>
/// Manages the Ultralight renderer and view lifecycle.
/// Stored as a <see cref="World"/> resource.
/// </summary>
public sealed class BrowserInstance : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Engine.Browser");

    private ULRenderer? _renderer;
    private View? _view;
    private bool _disposed;

    /// <summary>Current pixel width of the browser view.</summary>
    public uint Width { get; private set; }

    /// <summary>Current pixel height of the browser view.</summary>
    public uint Height { get; private set; }

    /// <summary>Whether the Ultralight surface has been painted since last GPU upload.</summary>
    public bool IsDirty { get; private set; }

    /// <summary>The underlying Ultralight view (null before <see cref="Initialize"/>).</summary>
    public View? View => _view;

    /// <summary>Row stride in bytes of the bitmap surface (may be &gt; Width*4 due to alignment).</summary>
    public uint SurfaceRowBytes => _view?.Surface?.RowBytes ?? (Width * 4);

    // ── Diagnostics (read by ImGui debug window) ─────────────────────
    public bool HasSurface => _view?.Surface is not null;
    public bool NeedsPaintNow => _view?.NeedsPaint ?? false;
    public int DiagUpdateCount { get; private set; }
    public int DiagPaintCount { get; private set; }
    public int DiagUploadCount { get; internal set; }
    public long DiagNonZeroPixels { get; private set; }
    public long DiagTotalPixels => Width * Height;

    /// <summary>Page title retrieved via JS — updated once per second.</summary>
    public string? DiagPageTitle { get; private set; }

    /// <summary>Hex representation of the first 16 bytes of the last surface read.</summary>
    public string? DiagFirstBytes { get; private set; }

    /// <summary>Whether the ICU data file exists on disk.</summary>
    public bool DiagIcuExists => File.Exists(Path.Combine(AppContext.BaseDirectory, "resources", "icudt67l.dat"));

    /// <summary>Whether the CA certificate file exists on disk.</summary>
    public bool DiagCaCertExists => File.Exists(Path.Combine(AppContext.BaseDirectory, "resources", "cacert.pem"));

    private int _titleQueryCountdown;

    /// <summary>
    /// Initializes the Ultralight platform, renderer, and view.
    /// Call once during Startup.
    /// </summary>
    public void Initialize(uint width, uint height)
    {
        Logger.Info($"BrowserInstance: Initializing Ultralight ({width}x{height})...");
        Width = width;
        Height = height;

        // ── Extract embedded resources to disk ────────────────────────
        ExtractEmbeddedResources();

        // ── Platform configuration via AppCore ────────────────────────
        AppCoreMethods.SetDefaultLogger(Path.Combine(AppContext.BaseDirectory, "ultralight.log"));
        AppCoreMethods.SetPlatformFontLoader();
        AppCoreMethods.SetPlatformFileSystem(AppContext.BaseDirectory);
        ULPlatform.ErrorGPUDriverNotSet = false;
        Logger.Info("AppCore platform font loader, file system, and logger enabled.");

        // ── Renderer ──────────────────────────────────────────────────
        _renderer = ULPlatform.CreateRenderer();

        // CreateRenderer() may reset ErrorWrongThread to true internally.
        // The engine's Update stage runs systems in parallel (thread pool),
        // so we must disable the managed thread-affinity check afterwards.
        ULPlatform.ErrorWrongThread = false;
        Logger.Info("Ultralight renderer created.");

        // ── View configuration (CPU/bitmap surface mode) ──────────────
        var viewConfig = new ULViewConfig
        {
            IsAccelerated = false,   // CPU rendering → bitmap surface
            IsTransparent = true,    // transparent background for compositing
            InitialDeviceScale = 1.0,
            InitialFocus = true,
            EnableImages = true,
            EnableJavaScript = true,
            FontFamilyStandard = "Arial",
            FontFamilyFixed = "Courier New",
            FontFamilySerif = "Times New Roman",
            FontFamilySansSerif = "Arial",
            UserAgent = "Mozilla/5.0 (3DEngine Browser) UltralightNet/1.3",
        };

        _view = _renderer.CreateView(width, height, viewConfig, _renderer.DefaultSession);
        _view.Focus();
        Logger.Info($"Ultralight view created ({width}x{height}, transparent={viewConfig.IsTransparent}).");
    }

    /// <summary>Loads raw HTML content into the view.</summary>
    public void LoadHtml(string html)
    {
        if (_view is null) return;
        Logger.Info("BrowserInstance: Loading HTML content...");
        _view.HTML = html;
    }

    /// <summary>Loads a URL into the view.</summary>
    public void LoadUrl(string url)
    {
        if (_view is null) return;
        Logger.Info($"BrowserInstance: Loading URL: {url}");
        _view.URL = url;
    }

    /// <summary>
    /// Evaluates JavaScript in the view context.
    /// Returns the result string or null on error.
    /// </summary>
    public string? EvaluateScript(string js)
    {
        if (_view is null) return null;
        var result = _view.EvaluateScript(js, out var exception);
        if (exception is not null)
        {
            Logger.Warn($"JS exception: {exception}");
            return null;
        }
        return result;
    }

    /// <summary>
    /// Advances Ultralight internal timers and triggers repaint when needed.
    /// Call once per frame (Update stage).
    /// </summary>
    public void Update()
    {
        if (_renderer is null) return;

        DiagUpdateCount++;

        // Query page title once per ~60 frames (roughly once per second)
        if (--_titleQueryCountdown <= 0)
        {
            _titleQueryCountdown = 60;
            DiagPageTitle = EvaluateScript("document.title");
        }

        // Update processes timers, JS, layout, network — may flag the view as needing paint.
        _renderer.Update();

        // Capture the dirty state BEFORE Render(), because Render() paints the
        // dirty regions to the bitmap surface and then clears the NeedsPaint flag.
        if (_view is not null && _view.NeedsPaint)
        {
            IsDirty = true;
            DiagPaintCount++;
        }

        // Render paints all dirty views to their surfaces.
        _renderer.Render();

        // Also check AFTER Render in case this SDK version sets NeedsPaint post-render.
        if (_view is not null && _view.NeedsPaint)
        {
            IsDirty = true;
            DiagPaintCount++;
        }
    }

    /// <summary>
    /// Locks the bitmap surface pixels and copies them into <paramref name="destination"/>.
    /// Returns true if new pixel data was copied (i.e. the surface was dirty).
    /// </summary>
    public unsafe bool TryGetPixels(Span<byte> destination, out uint rowBytes)
    {
        rowBytes = 0;
        if (_view?.Surface is not { } surface)
            return false;

        if (!IsDirty)
            return false;

        rowBytes = surface.RowBytes;
        var byteCount = (int)(rowBytes * Height);
        if (destination.Length < byteCount)
            return false;

        var ptr = surface.LockPixels();
        try
        {
            var src = new ReadOnlySpan<byte>(ptr, byteCount);
            src.CopyTo(destination);

            // Capture first 16 bytes for diagnostics
            var previewLen = Math.Min(16, byteCount);
            DiagFirstBytes = Convert.ToHexString(src.Slice(0, previewLen));

            // Scan for non-zero pixels (diagnostics)
            long nonZero = 0;
            for (int i = 0; i < byteCount; i += 4)
            {
                if (src[i] != 0 || src[i + 1] != 0 || src[i + 2] != 0 || src[i + 3] != 0)
                    nonZero++;
            }
            DiagNonZeroPixels = nonZero;
        }
        finally
        {
            surface.UnlockPixels();
        }

        surface.ClearDirtyBounds();
        IsDirty = false;
        return true;
    }

    /// <summary>
    /// Gets a read-only span over the raw bitmap surface pixels (locks internally).
    /// Caller must call <see cref="UnlockPixels"/> when done.
    /// Returns an empty span if there's no surface or it's not dirty.
    /// </summary>
    public unsafe ReadOnlySpan<byte> LockPixels(out uint rowBytes)
    {
        rowBytes = 0;
        if (_view?.Surface is not { } surface)
            return ReadOnlySpan<byte>.Empty;

        rowBytes = surface.RowBytes;
        var ptr = surface.LockPixels();
        return new ReadOnlySpan<byte>(ptr, (int)(rowBytes * Height));
    }

    /// <summary>Unlocks the bitmap surface pixels after a <see cref="LockPixels"/> call.</summary>
    public void UnlockPixels()
    {
        _view?.Surface?.UnlockPixels();
    }

    /// <summary>Clears the dirty flag and dirty bounds after uploading to GPU.</summary>
    public void ClearDirty()
    {
        _view?.Surface?.ClearDirtyBounds();
        IsDirty = false;
    }

    /// <summary>Resizes the browser view. No-op if size hasn't changed.</summary>
    public void Resize(uint width, uint height)
    {
        if (_view is null || (Width == width && Height == height)) return;
        if (width == 0 || height == 0) return;

        Logger.Info($"BrowserInstance: Resizing to {width}x{height}...");
        _view.Resize(in width, in height);
        Width = width;
        Height = height;
        IsDirty = true;
    }

    /// <summary>Fires a mouse event into the Ultralight view.</summary>
    public void FireMouseEvent(ULMouseEvent evt)
    {
        _view?.FireMouseEvent(evt);
    }

    /// <summary>Fires a key event into the Ultralight view.</summary>
    public void FireKeyEvent(ULKeyEvent evt)
    {
        _view?.FireKeyEvent(evt);
    }

    /// <summary>Fires a scroll event into the Ultralight view.</summary>
    public void FireScrollEvent(ULScrollEvent evt)
    {
        _view?.FireScrollEvent(evt);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Logger.Info("BrowserInstance: Disposing Ultralight resources...");
        _view?.Dispose();
        _view = null;
        _renderer?.Dispose();
        _renderer = null;
        Logger.Info("BrowserInstance disposed.");
    }

    /// <summary>
    /// Extracts the embedded Ultralight resources (ICU data, CA certs)
    /// from the managed assembly into a <c>resources/</c> directory on disk
    /// so the native AppCore file system can find them.
    /// </summary>
    private static void ExtractEmbeddedResources()
    {
        var resourceDir = Path.Combine(AppContext.BaseDirectory, "resources");
        Directory.CreateDirectory(resourceDir);

        ExtractIfMissing(resourceDir, "icudt67l.dat", () => Resources.Icudt67ldat);
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


