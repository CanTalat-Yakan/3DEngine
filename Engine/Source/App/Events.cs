namespace Engine;

/// <summary>Static helpers to get or create typed event queues in the world.</summary>
public static class Events
{
    /// <summary>Gets the event queue resource for T, inserting a new one if missing.</summary>
    public static Events<T> Get<T>(World world)
    {
        var evt = world.TryResource<Events<T>>();
        if (evt is null)
        {
            evt = new Events<T>();
            world.InsertResource(evt);
        }

        return evt;
    }
}

/// <summary>Event queue resource per event type T stored in World.</summary>
public sealed class Events<T>
{
    private readonly Queue<T> _queue = new();
    private readonly List<T> _drainBuffer = new();

    /// <summary>Queues an event for later consumption.</summary>
    public void Send(T evt) => _queue.Enqueue(evt);

    /// <summary>Attempts to read and remove the next event; returns false if none available.</summary>
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

    /// <summary>Drains the queue into a reusable buffer and returns its enumerable view.</summary>
    public IEnumerable<T> Drain()
    {
        _drainBuffer.Clear();
        while (_queue.Count > 0)
            _drainBuffer.Add(_queue.Dequeue());
        return _drainBuffer;
    }
}

/// <summary>Write-only wrapper for sending events of type T.</summary>
public readonly ref struct EventWriter<T>
{
    private readonly Events<T> _evt;
    public EventWriter(Events<T> evt) => _evt = evt;
    public void Send(T evt) => _evt.Send(evt);
    public static EventWriter<T> Get(World world) => new(Events.Get<T>(world));
}

/// <summary>Read-only wrapper for draining events of type T.</summary>
public readonly ref struct EventReader<T>
{
    private readonly Events<T> _evt;
    public EventReader(Events<T> evt) => _evt = evt;
    public IEnumerable<T> Drain() => _evt.Drain();
    public static EventReader<T> Get(World world) => new(Events.Get<T>(world));
}
