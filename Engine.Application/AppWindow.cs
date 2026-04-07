using SDL3;

namespace Engine;

/// <summary>Thin wrapper over SDL window and event loop with hooks for resize, quit, and raw event forwarding.</summary>
public sealed class AppWindow
{
    /// <summary>The underlying SDL window/renderer pair wrapper.</summary>
    public SdlWindow Sdl { get; private set; }

    public delegate void ResizeEventHandler(int width, int height);
    /// <summary>Raised when this window is resized.</summary>
    public event ResizeEventHandler? ResizeEvent;

    /// <summary>Raised when an SDL Quit or this window is closed.</summary>
    public event Action? QuitEvent;

    /// <summary>Raised for every SDL event polled; allows input systems to consume events.</summary>
    public event Action<SDL.Event>? SDLEvent;

    private volatile bool _shouldClose;

    public AppWindow(WindowData windowData, GraphicsBackend backend)
    {
        var useVulkan = backend == GraphicsBackend.Vulkan;
        Sdl = new(windowData.Title, windowData.Width, windowData.Height, useVulkan);
    }

    public AppWindow(WindowData windowData) : this(windowData, GraphicsBackend.Sdl) {}

    /// <summary>Returns true if this window has keyboard focus.</summary>
    public bool IsFocused()
    {
        return SDL.GetKeyboardFocus() == Sdl.Window;
    }

    /// <summary>Shows the window and applies an initial command.</summary>
    public void Show(WindowCommand command = WindowCommand.Normal)
    {
        SDL.ShowWindow(Sdl.Window);
        switch (command)
        {
            case WindowCommand.Maximize:
                SDL.MaximizeWindow(Sdl.Window);
                break;
            case WindowCommand.Minimize:
                SDL.MinimizeWindow(Sdl.Window);
                break;
            case WindowCommand.Restore:
                SDL.RestoreWindow(Sdl.Window);
                break;
            case WindowCommand.Hide:
                SDL.HideWindow(Sdl.Window);
                break;
            default:
                break;
        }
    }

    /// <summary>Requests the main loop to exit after the current iteration.</summary>
    public void RequestClose() => _shouldClose = true;

    /// <summary>Pumps SDL events and calls the supplied per-frame delegates until quit is requested.</summary>
    /// <remarks>
    /// Resize events are coalesced: if multiple <c>WindowResized</c> events arrive in
    /// a single poll batch, only the final dimensions are dispatched — once — after the
    /// batch drains.  This collapses N resize callbacks per frame to at most 1.
    /// </remarks>
    public void Looping(params Delegate[] onFrame)
    {
        bool running = true;
        while (running)
        {
            if (_shouldClose) running = false;

            // ── Coalesced resize state for this poll batch ──
            bool resizedThisBatch = false;
            int coalescedW = 0, coalescedH = 0;

            while (SDL.PollEvent(out var e))
            {
                // ── HiDPI: scale mouse coordinates from window-space to content-space ──
                // SDL gives coords in window logical coords (0..windowLogicalW).
                // Content (WebView/ImGui) uses Width×Height (the config resolution).
                var evtType = (SDL.EventType)e.Type;
                if (evtType == SDL.EventType.MouseMotion ||
                    evtType is SDL.EventType.MouseButtonDown or SDL.EventType.MouseButtonUp)
                {
                    SDL.GetWindowSize(Sdl.Window, out int winW, out int winH);
                    float scaleX = (winW > 0) ? (float)Sdl.Width / winW : 1f;
                    float scaleY = (winH > 0) ? (float)Sdl.Height / winH : 1f;

                    if (evtType == SDL.EventType.MouseMotion)
                    {
                        e.Motion.X *= scaleX;
                        e.Motion.Y *= scaleY;
                        e.Motion.XRel *= scaleX;
                        e.Motion.YRel *= scaleY;
                    }
                    else
                    {
                        e.Button.X *= scaleX;
                        e.Button.Y *= scaleY;
                    }
                }

                SDLEvent?.Invoke(e);

                if ((SDL.EventType)e.Type == SDL.EventType.Quit)
                {
                    QuitEvent?.Invoke();
                    running = false;
                }
                if ((SDL.EventType)e.Type == SDL.EventType.WindowCloseRequested && e.Window.WindowID == SDL.GetWindowID(Sdl.Window))
                {
                    QuitEvent?.Invoke();
                    running = false;
                }
                if ((SDL.EventType)e.Type == SDL.EventType.WindowResized && e.Window.WindowID == SDL.GetWindowID(Sdl.Window))
                {
                    // Update display scale (may change if window moved between monitors).
                    float resizeScale = SDL.GetWindowDisplayScale(Sdl.Window);
                    if (resizeScale <= 0f) resizeScale = 1f;
                    Sdl.DisplayScale = resizeScale;

                    // Flag for coalesced dispatch after the poll batch drains.
                    // Only the last (most recent) dimensions matter.
                    SDL.GetWindowSize(Sdl.Window, out coalescedW, out coalescedH);
                    resizedThisBatch = true;
                }
            }

            // ── Dispatch the single coalesced resize (if any) ──
            if (resizedThisBatch && coalescedW > 0 && coalescedH > 0)
            {
                ResizeEvent?.Invoke(coalescedW, coalescedH);
            }

            foreach (var frame in onFrame)
                frame?.DynamicInvoke();
        }
    }

    /// <summary>Disposes the underlying SDL resources, optionally invoking a callback before return.</summary>
    public void Dispose(Action? onDispose)
    {
        Sdl.Destroy();
        onDispose?.Invoke();
    }
}
