using SDL3;

namespace Engine;

/// <summary>Frame-based input state (keys, mouse, deltas) derived from SDL events.</summary>
public sealed class SdlInput
{
    private readonly HashSet<SDL.Scancode> _down = new();
    private readonly HashSet<SDL.Scancode> _pressed = new();
    private readonly HashSet<SDL.Scancode> _released = new();

    public int MouseX { get; private set; }
    public int MouseY { get; private set; }
    public int MouseDeltaX { get; private set; }
    public int MouseDeltaY { get; private set; }
    public SDL.MouseButtonFlags MouseButtons { get; private set; }

    public bool KeyDown(Key code) => _down.Contains((SDL.Scancode)code);
    public bool KeyPressed(Key code) => _pressed.Contains((SDL.Scancode)code);
    public bool KeyReleased(Key code) => _released.Contains((SDL.Scancode)code);

    public void BeginFrame()
    {
        _pressed.Clear();
        _released.Clear();
        MouseDeltaX = 0; MouseDeltaY = 0;
    }

    public void EndFrame() { }

    public void Process(SDL.Event evt)
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