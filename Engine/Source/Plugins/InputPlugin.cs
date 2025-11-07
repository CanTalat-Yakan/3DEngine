using SDL3;

namespace Engine;

/// <summary>Event: a key transitioned to down this frame.</summary>
public readonly record struct KeyPressed(SDL.Scancode Scancode);

/// <summary>Event: window size changed.</summary>
public readonly record struct WindowResized(int Width, int Height);

/// <summary>Installs Input resource, wires SDL events to it, and publishes key/resize events.</summary>
public sealed class InputPlugin : IPlugin
{
    /// <summary>Inserts the Input resource and connects SDL event handlers and per-frame hooks.</summary>
    public void Build(App app)
    {
        var input = new Input();
        app.World.InsertResource(input);

        app.World.Resource<AppWindow>().SDLEvent += (evt) =>
        {
            if ((SDL.EventType)evt.Type == SDL.EventType.KeyDown)
            {
                var kd = evt.Key;
                if (!input.KeyDown(kd.Scancode))
                    Events.Get<KeyPressed>(app.World).Send(new KeyPressed(kd.Scancode));
            }
            input.Process(evt);
        };
        app.World.Resource<AppWindow>().ResizeEvent += (w, h) => Events.Get<WindowResized>(app.World).Send(new WindowResized(w, h));

        app.AddSystem(Stage.First, (world) => world.Resource<Input>().BeginFrame());
        app.AddSystem(Stage.Last, (world) => world.Resource<Input>().EndFrame());
    }
}

/// <summary>Frame-based input state (keys, mouse, deltas) derived from SDL events.</summary>
public sealed class Input
{
    private readonly HashSet<SDL.Scancode> _down = new();
    private readonly HashSet<SDL.Scancode> _pressed = new();
    private readonly HashSet<SDL.Scancode> _released = new();

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }
    public int MouseDeltaX { get; private set; }
    public int MouseDeltaY { get; private set; }
    public SDL.MouseButtonFlags MouseButtons { get; private set; }

    public bool KeyDown(SDL.Scancode code) => _down.Contains(code);
    public bool KeyPressed(SDL.Scancode code) => _pressed.Contains(code);
    public bool KeyReleased(SDL.Scancode code) => _released.Contains(code);

    internal void BeginFrame()
    {
        _pressed.Clear();
        _released.Clear();
        MouseDeltaX = 0; MouseDeltaY = 0;
    }

    internal void EndFrame() { }

    internal void Process(SDL.Event evt)
    {
        switch ((SDL.EventType)evt.Type)
        {
            case SDL.EventType.KeyDown:
                var keyDown = evt.Key;
                if (!_down.Contains(keyDown.Scancode)) _pressed.Add(keyDown.Scancode);
                _down.Add(keyDown.Scancode);
                break;
            case SDL.EventType.KeyUp:
                var keyUp = evt.Key;
                _down.Remove(keyUp.Scancode);
                _released.Add(keyUp.Scancode);
                break;
            case SDL.EventType.MouseMotion:
                MouseDeltaX += (int)evt.Motion.XRel;
                MouseDeltaY += (int)evt.Motion.YRel;
                MouseX = (int)evt.Motion.X;
                MouseY = (int)evt.Motion.Y;
                break;
            case SDL.EventType.MouseButtonDown:
            case SDL.EventType.MouseButtonUp:
                MouseButtons = SDL.GetMouseState(out float x, out float y);
                MouseX = (int)x; MouseY = (int)y;
                break;
        }
    }
}
