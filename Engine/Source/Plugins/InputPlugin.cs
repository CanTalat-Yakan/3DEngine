using SDL3;

namespace Engine;

public sealed class InputPlugin : IPlugin
{
    public void Build(App app)
    {
        var input = new Input();
        app.World.InsertResource(input);

        app.World.Resource<AppWindow>().SDLEvent += (e) =>
        {
            if ((SDL.EventType)e.Type == SDL.EventType.KeyDown)
            {
                var kd = e.Key;
                if (!input.KeyDown(kd.Scancode))
                    Events.Get<KeyPressed>(app.World).Send(new KeyPressed(kd.Scancode));
            }
            input.Process(e);
        };
        app.World.Resource<AppWindow>().ResizeEvent += (w, h) => Events.Get<WindowResized>(app.World).Send(new WindowResized(w, h));

        app.AddSystem(Stage.First, (World w) => w.Resource<Input>().BeginFrame());
        app.AddSystem(Stage.Last, (World w) => w.Resource<Input>().EndFrame());
    }
}

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

    internal void Process(SDL.Event e)
    {
        switch ((SDL.EventType)e.Type)
        {
            case SDL.EventType.KeyDown:
                var kd = e.Key;
                if (!_down.Contains(kd.Scancode)) _pressed.Add(kd.Scancode);
                _down.Add(kd.Scancode);
                break;
            case SDL.EventType.KeyUp:
                var ku = e.Key;
                _down.Remove(ku.Scancode);
                _released.Add(ku.Scancode);
                break;
            case SDL.EventType.MouseMotion:
                MouseDeltaX += (int)e.Motion.XRel;
                MouseDeltaY += (int)e.Motion.YRel;
                MouseX = (int)e.Motion.X;
                MouseY = (int)e.Motion.Y;
                break;
            case SDL.EventType.MouseButtonDown:
            case SDL.EventType.MouseButtonUp:
                MouseButtons = SDL.GetMouseState(out float x, out float y);
                MouseX = (int)x; MouseY = (int)y;
                break;
        }
    }
}
