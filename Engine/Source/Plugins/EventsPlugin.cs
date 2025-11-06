namespace Engine;

public sealed class EventsPlugin : IPlugin
{
    public void Build(App app)
    {
        // No default events inserted; users can call Events<T>.Get on the World as needed.
    }
}

public readonly record struct KeyPressed(SDL3.SDL.Scancode Scancode);
public readonly record struct WindowResized(int Width, int Height);

/// <summary>
/// A Bevy-like event queue resource per event type T. Stored in World as Events<T>.
/// </summary>
public sealed class Events<T>
{
    private readonly Queue<T> _queue = new();
    private readonly List<T> _drainBuffer = new();

    public void Send(T evt) => _queue.Enqueue(evt);

    public bool TryRead(out T evt)
    {
        if (_queue.Count > 0)
        {
            evt = _queue.Dequeue();
            return true;
        }
        evt = default!;
        return false;
    }

    public IEnumerable<T> Drain()
    {
        _drainBuffer.Clear();
        while (_queue.Count > 0)
            _drainBuffer.Add(_queue.Dequeue());
        return _drainBuffer;
    }
}
