using System.Runtime.InteropServices;
using SDL3;

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
                    input.AddMouseDelta((int)e.Motion.XRel, (int)e.Motion.YRel);
                    break;
                case SDL.EventType.MouseButtonDown:
                case SDL.EventType.MouseButtonUp:
                    int btn = e.Button.Button - 1; // SDL 1..5 -> 0..4
                    if (btn is >= 0 and < 5)
                        input.SetMouseButton((MouseButton)btn, (SDL.EventType)e.Type == SDL.EventType.MouseButtonDown);
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
                    var mapped = (Key)e.Key.Scancode;
                    if (mapped != Key.Unknown)
                        input.SetKey(mapped, down);
                    break;
            }
        }
    }

    /// <summary>Creates and shows the AppWindow using Config, inserts AppWindow, and provides the main loop driver.</summary>
    public void Build(App app)
    {
        var logger = Log.Category("Engine.Window");
        var config = app.World.Resource<Config>();

        logger.Info($"AppWindowPlugin: Creating window \"{config.WindowData.Title}\" ({config.WindowData.Width}x{config.WindowData.Height}) with backend={config.Graphics}...");
        var window = new AppWindow(config.WindowData, config.Graphics);

        logger.Info($"Showing window with command: {config.WindowCommand}");
        window.Show(config.WindowCommand);

        app.World.InsertResource(window);
        app.World.InsertResource<IMainLoopDriver>(new SdlMainLoopDriver(window));
        app.World.InsertResource<IInputBackend>(new SdlInputBackend());
        logger.Info("AppWindow, IMainLoopDriver, and IInputBackend resources registered.");

        // When Vulkan is selected, provide the surface source so the renderer plugin can initialize.
        if (config.Graphics == GraphicsBackend.Vulkan)
        {
            app.World.InsertResource<ISurfaceSource>(new SdlSurfaceSource(window.Sdl));
            logger.Info("Vulkan surface source (SdlSurfaceSource) registered for renderer.");
        }
    }
}
