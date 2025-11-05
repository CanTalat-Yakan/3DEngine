using SDL3;

namespace Engine;

public sealed partial class AppWindow
{
    public SDLWindow SdlWindow { get; private set; }

    public delegate void ResizeEventHandler(int width, int height);
    public event ResizeEventHandler? ResizeEvent;

    // Fired when an SDL Quit or this window is closed
    public event Action? QuitEvent;

    // Broadcast each SDL event to subscribers (input, custom handlers)
    public event Action<SDL.Event>? SdlEvent;

    private volatile bool _shouldClose;

    public AppWindow(WindowData windowData)
    {
        SdlWindow = new(windowData.Title, windowData.Width, windowData.Height);
    }

    public bool IsFocused()
    {
        return SDL.GetKeyboardFocus() == SdlWindow.Window;
    }

    public void Show(WindowCommand command = WindowCommand.Normal)
    {
        SDL.ShowWindow(SdlWindow.Window);
        // Map WindowCommand to SDL; avoid duplicate labels with same enum value.
        switch (command)
        {
            case WindowCommand.Maximize:
                SDL.MaximizeWindow(SdlWindow.Window);
                break;
            case WindowCommand.Minimize:
                SDL.MinimizeWindow(SdlWindow.Window);
                break;
            case WindowCommand.Restore:
                SDL.RestoreWindow(SdlWindow.Window);
                break;
            case WindowCommand.Hide:
                SDL.HideWindow(SdlWindow.Window);
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
                SdlEvent?.Invoke(e);

                // Forward events to ImGui input adapter
                GUIInput.ProcessEvent(e);

                if ((SDL.EventType)e.Type == SDL.EventType.Quit)
                {
                    QuitEvent?.Invoke();
                    running = false;
                }
                if ((SDL.EventType)e.Type == SDL.EventType.WindowCloseRequested && e.Window.WindowID == SDL.GetWindowID(SdlWindow.Window))
                {
                    QuitEvent?.Invoke();
                    running = false;
                }
                if ((SDL.EventType)e.Type == SDL.EventType.WindowResized && e.Window.WindowID == SDL.GetWindowID(SdlWindow.Window))
                {
                    SDL.GetWindowSize(SdlWindow.Window, out int w, out int h);
                    SdlWindow.Width = w; SdlWindow.Height = h;
                    ResizeEvent?.Invoke(w, h);
                }
            }

            foreach (var frame in onFrame)
                frame?.DynamicInvoke();
        }
    }

    public void Dispose(Action? onDispose)
    {
        SdlWindow.Destroy();
        onDispose?.Invoke();
    }
}
