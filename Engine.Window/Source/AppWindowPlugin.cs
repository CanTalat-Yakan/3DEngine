using System.Runtime.InteropServices;
using SDL3;
using static SDL3.SDL;

namespace Engine;

/// <summary>Creates the application window from Config and inserts it as a resource.</summary>
public sealed class AppWindowPlugin : IPlugin
{
    private sealed class SdlMainLoopDriver(AppWindow window) : IMainLoopDriver
    {
        public void Run(Action frameStep)
        {
            window.Looping((Action)(() => frameStep()));
            window.Dispose(null);
        }
    }

    private sealed class SdlInputBackend : IInputBackend
    {
        public void Initialize(App app, Input input)
        {
            // Hook SDL events to update input; AppWindow already pumps events but we add handlers.
            var win = app.World.Resource<AppWindow>();
            win.SDLEvent += e => ProcessInputEvent(e, input);
        }

        private static void ProcessInputEvent(SDL.Event e, Input input)
        {
            switch ((SDL.EventType)e.Type)
            {
                case SDL.EventType.MouseMotion:
                    input.SetMousePosition((int)e.Motion.X, (int)e.Motion.Y);
                    break;
                case SDL.EventType.MouseButtonDown:
                case SDL.EventType.MouseButtonUp:
                    int btn = e.Button.Button - 1; // SDL 1..5 -> 0..4
                    if (btn >= 0 && btn < 5)
                        input.SetMouseButton(btn, (SDL.EventType)e.Type == SDL.EventType.MouseButtonDown);
                    break;
                case SDL.EventType.MouseWheel:
                    input.AddWheel(e.Wheel.X, e.Wheel.Y);
                    break;
                case SDL.EventType.TextInput:
                    if (e.Text.Text != IntPtr.Zero)
                    {
                        var s = Marshal.PtrToStringUTF8(e.Text.Text);
                        if (!string.IsNullOrEmpty(s)) input.AddText(s);
                    }
                    break;
                case SDL.EventType.KeyDown:
                case SDL.EventType.KeyUp:
                    bool down = (SDL.EventType)e.Type == SDL.EventType.KeyDown;
                    var sc = e.Key.Scancode;
                    var mapped = (Key)sc;
                    if (mapped != Key.Unknown)
                        input.SetKey(mapped, down);
                    break;
            }
        }
    }

    /// <summary>Creates and shows the AppWindow using Config, inserts AppWindow, and provides the main loop driver.</summary>
    public void Build(App app)
    {
        var config = app.World.Resource<Config>();
        var window = new AppWindow(config.WindowData, config.Graphics);
        window.Show(config.WindowCommand);
        app.World.InsertResource(window);
        app.World.InsertResource<IMainLoopDriver>(new SdlMainLoopDriver(window));
        // Provide SDL input backend for core InputPlugin to initialize.
        app.World.InsertResource<IInputBackend>(new SdlInputBackend());
    }
}

/// <summary>Provides application exit handling: listens for window quit events and requests closure via a resource flag.</summary>
public sealed class AppExitPlugin : IPlugin
{
    /// <summary>Inserts the <see cref="AppExit"/> resource (if missing), wires window quit to set its flag, and adds a First-stage system to close the window when requested.</summary>
    public void Build(App app)
    {
        // Ensure exit state resource exists.
        if (!app.World.ContainsResource<AppExit>())
            app.World.InsertResource(new AppExit());

        // When the window signals quit, raise the Requested flag.
        var window = app.World.Resource<AppWindow>();
        window.QuitEvent += () => app.World.Resource<AppExit>().Requested = true;

        // Early frame: if an exit was requested previously, ask window to close (will break main loop).
        app.AddSystem(Stage.First, (World world) =>
        {
            if (world.Resource<AppExit>().Requested)
                world.Resource<AppWindow>().RequestClose();
        });
    }
}

/// <summary>Resource tracking whether an application exit was requested.</summary>
public sealed class AppExit
{
    /// <summary>True if a quit event was observed and the app should close.</summary>
    public bool Requested;
}
