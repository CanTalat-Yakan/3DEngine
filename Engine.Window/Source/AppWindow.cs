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

    public AppWindow(WindowData windowData) : this(windowData, GraphicsBackend.SdlRenderer) {}

    /// <summary>Returns true if this window has keyboard focus.</summary>
    public bool IsFocused()
    {
        return SDL.GetKeyboardFocus() == Sdl.Window;
    }

    /// <summary>
    /// Shows the window and applies an initial command (Show/Maximize/Minimize/etc.).
    ///</summary>
    public void Show(WindowCommand command = WindowCommand.Normal)
    {
        SDL.ShowWindow(Sdl.Window);
        // Map WindowCommand to SDL; avoid duplicate labels with same enum value.
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

    /// <summary>
    /// Pumps SDL events and calls the supplied per-frame delegates until quit is requested.
    ///</summary>
    public void Looping(params Delegate[] onFrame)
    {
        bool running = true;
        while (running)
        {
            if (_shouldClose) running = false;

            while (SDL.PollEvent(out var e))
            {
                // Broadcast raw event
                SDLEvent?.Invoke(e);

                // Forward events to ImGui input adapter
                SdlImGuiInput.ProcessEvent(e);

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
                    SDL.GetWindowSize(Sdl.Window, out int w, out int h);
                    Sdl.Width = w; Sdl.Height = h;
                    ResizeEvent?.Invoke(w, h);
                }
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
