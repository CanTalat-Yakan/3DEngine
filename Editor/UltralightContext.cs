using UltralightNet;
using UltralightNet.AppCore;

namespace Engine;

/// <summary>
/// Manages the Ultralight renderer, session, and view lifecycle.
/// Renders web content to a CPU bitmap surface that can be uploaded to the GPU each frame.
/// </summary>
public sealed class UltralightContext : IDisposable
{
    private static readonly ILogger Logger = Log.Category("Editor.Ultralight");

    private UltralightNet.Renderer? _renderer;
    private Session? _session;
    private View? _view;
    private bool _disposed;

    public uint Width { get; private set; }
    public uint Height { get; private set; }
    public View? View => _view;
    public bool IsLoaded => _view is not null && !_view.IsLoading;

    /// <summary>
    /// Initializes the Ultralight platform and creates a renderer, session, and view.
    /// </summary>
    public UltralightContext(uint width, uint height, string url, bool transparent = true)
    {
        Width = Math.Max(1, width);
        Height = Math.Max(1, height);

        Logger.Info($"Configuring Ultralight platform (CPU surface, transparent={transparent})...");

        // Configure platform handlers
        ULPlatform.ErrorMissingResources = false;
        ULPlatform.ErrorGPUDriverNotSet = false;
        ULPlatform.ErrorWrongThread = false;
        ULPlatform.EnableDefaultLogger = true;
        ULPlatform.SetDefaultFileSystem = true;
        ULPlatform.SetDefaultFontLoader = true;
        Logger.Debug("Ultralight platform flags set (default logger, file system, font loader).");

        // Create Ultralight renderer with default config
        var config = new ULConfig
        {
            ResourcePathPrefix = "./resources/",
            ForceRepaint = true,
        };

        Logger.Debug("Creating Ultralight renderer...");
        _renderer = ULPlatform.CreateRenderer(config, dispose: true);

        Logger.Debug("Creating Ultralight session (persistent=false, name='editor')...");
        _session = _renderer.CreateSession(false, "editor");

        // Create the view with the desired settings
        var viewConfig = new ULViewConfig
        {
            IsAccelerated = false,   // CPU bitmap surface
            IsTransparent = transparent,
            EnableJavaScript = true,
            EnableImages = true,
            InitialDeviceScale = 1.0,
            InitialFocus = true,
        };

        Logger.Debug($"Creating Ultralight view ({Width}x{Height}, JS=true, images=true, scale=1.0)...");
        _view = _renderer.CreateView(Width, Height, viewConfig, _session);

        // Register console message handler for debugging
        _view.OnAddConsoleMessage += (source, level, message, line, col, sourceId) =>
        {
            var msg = $"[JS {level}] {message} ({sourceId}:{line}:{col})";
            if (level == ULMessageLevel.Error)
                Logger.Error(msg);
            else if (level == ULMessageLevel.Warning)
                Logger.Warn(msg);
            else
                Logger.Debug(msg);
        };

        _view.OnFailLoading += (frameId, isMainFrame, urlStr, description, errorDomain, errorCode) =>
        {
            Logger.Error($"Page load failed: {description} (domain={errorDomain}, code={errorCode}, url={urlStr}, frame={frameId}, main={isMainFrame})");
        };

        _view.OnFinishLoading += (frameId, isMainFrame, urlStr) =>
        {
            Logger.Info($"Page loaded: {urlStr} (frame={frameId}, main={isMainFrame})");
        };

        // Navigate to the requested URL
        Logger.Info($"Navigating to: {url}");
        _view.URL = url;
        _view.Focus();
        Logger.Info($"UltralightContext initialized — view {Width}x{Height}, awaiting page load.");
    }

    /// <summary>Ticks Ultralight's internal timers and JavaScript execution.</summary>
    public void Update()
    {
        _renderer?.Update();
    }

    /// <summary>Renders the current view to its bitmap surface.</summary>
    public void Render()
    {
        _renderer?.Render();
    }

    /// <summary>
    /// Copies the view's bitmap surface pixels into the provided span.
    /// Returns true if pixels were successfully copied.
    /// </summary>
    public unsafe bool TryGetPixels(out ReadOnlySpan<byte> pixels, out uint rowBytes)
    {
        pixels = default;
        rowBytes = 0;

        if (_view is null) return false;

        var surface = _view.Surface;
        if (surface is null) return false;

        var bitmap = surface.Value.Bitmap;
        if (bitmap.IsEmpty) return false;

        rowBytes = bitmap.RowBytes;
        var ptr = bitmap.LockPixels();
        if (ptr == null)
        {
            bitmap.UnlockPixels();
            return false;
        }

        var size = (int)bitmap.Size;
        pixels = new ReadOnlySpan<byte>(ptr, size);
        return true;
    }

    /// <summary>Unlocks the bitmap surface after pixel reading is complete.</summary>
    public void UnlockPixels()
    {
        if (_view?.Surface is { } surface)
        {
            surface.Bitmap.UnlockPixels();
        }
    }

    /// <summary>Resizes the Ultralight view to new dimensions.</summary>
    public void Resize(uint width, uint height)
    {
        width = Math.Max(1, width);
        height = Math.Max(1, height);

        if (width == Width && height == Height) return;

        Logger.Info($"Resizing Ultralight view: {Width}x{Height} → {width}x{height}");
        Width = width;
        Height = height;

        _view?.Resize(in width, in height);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        Logger.Info("Disposing UltralightContext (view, session, renderer)...");
        _view?.Dispose();
        _view = null;

        _session?.Dispose();
        _session = null;

        _renderer?.Dispose();
        _renderer = null;
        Logger.Info("UltralightContext disposed.");
    }
}

