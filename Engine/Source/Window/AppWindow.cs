using SDL3;

namespace Engine;

public sealed class AppWindow
{
    public SDLWindow SDLWindow { get; private set; }

    public delegate void ResizeEventHandler(int width, int height);
    public event ResizeEventHandler? ResizeEvent;

    // Fired when an SDL Quit or this window is closed
    public event Action? QuitEvent;

    // Broadcast each SDL event to subscribers (input, custom handlers)
    public event Action<SDL.Event>? SDLEvent;

    private volatile bool _shouldClose;

    public AppWindow(WindowData windowData)
    {
        SDLWindow = new(windowData.Title, windowData.Width, windowData.Height);
    }

    public bool IsFocused()
    {
        return SDL.GetKeyboardFocus() == SDLWindow.Window;
    }

    public void Show(WindowCommand command = WindowCommand.Normal)
    {
        SDL.ShowWindow(SDLWindow.Window);
        // Map WindowCommand to SDL; avoid duplicate labels with same enum value.
        switch (command)
        {
            case WindowCommand.Maximize:
                SDL.MaximizeWindow(SDLWindow.Window);
                break;
            case WindowCommand.Minimize:
                SDL.MinimizeWindow(SDLWindow.Window);
                break;
            case WindowCommand.Restore:
                SDL.RestoreWindow(SDLWindow.Window);
                break;
            case WindowCommand.Hide:
                SDL.HideWindow(SDLWindow.Window);
                break;
            default:
                break;
        }
    }

    public void RequestClose() => _shouldClose = true;

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
                GUIInput.ProcessEvent(e);

                if ((SDL.EventType)e.Type == SDL.EventType.Quit)
                {
                    QuitEvent?.Invoke();
                    running = false;
                }
                if ((SDL.EventType)e.Type == SDL.EventType.WindowCloseRequested && e.Window.WindowID == SDL.GetWindowID(SDLWindow.Window))
                {
                    QuitEvent?.Invoke();
                    running = false;
                }
                if ((SDL.EventType)e.Type == SDL.EventType.WindowResized && e.Window.WindowID == SDL.GetWindowID(SDLWindow.Window))
                {
                    SDL.GetWindowSize(SDLWindow.Window, out int w, out int h);
                    SDLWindow.Width = w; SDLWindow.Height = h;
                    ResizeEvent?.Invoke(w, h);
                }
            }

            foreach (var frame in onFrame)
                frame?.DynamicInvoke();
        }
    }

    public void Dispose(Action? onDispose)
    {
        SDLWindow.Destroy();
        onDispose?.Invoke();
    }
}
