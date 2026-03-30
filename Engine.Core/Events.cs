using System.Runtime.InteropServices;

namespace Engine;

/// <summary>Static helpers to get or create typed event queues in the world.</summary>
public static class Events
{
    /// <summary>Gets the event queue resource for T, inserting a new one if missing.</summary>
    public static Events<T> Get<T>(World world)
        => world.TryResource<Events<T>>() ?? Create<T>(world);

    private static Events<T> Create<T>(World world)
    {
        var events = new Events<T>();
        world.InsertResource(events);
        return events;
    }
}

/// <summary>
/// Thread-safe event queue per event type T, stored as a <see cref="World"/> resource.
/// Events accumulate during a frame and should be cleared once per frame
/// (typically in the <see cref="Stage.Cleanup"/> stage) via <see cref="Clear"/>.
/// </summary>
public sealed class Events<T>
{
    private readonly Lock _lock = new();
    private readonly List<T> _buffer = [];

    /// <summary>Number of buffered events.</summary>
    public int Count { get { lock (_lock) return _buffer.Count; } }

    /// <summary>True when the buffer is empty.</summary>
    public bool IsEmpty { get { lock (_lock) return _buffer.Count == 0; } }

    /// <summary>Queues a single event.</summary>
    public void Send(T evt)
    {
        lock (_lock) _buffer.Add(evt);
    }

    /// <summary>Queues multiple events at once.</summary>
    public void SendBatch(ReadOnlySpan<T> events)
    {
        lock (_lock)
        {
            foreach (var evt in events)
                _buffer.Add(evt);
        }
    }

    /// <summary>
    /// Returns a span view of buffered events for zero-allocation iteration.
    /// <b>Not safe while other threads call <see cref="Send"/>.</b>
    /// Use only from stages that do not overlap with writers.
    /// </summary>
    public ReadOnlySpan<T> AsSpan() => CollectionsMarshal.AsSpan(_buffer);

    /// <summary>Returns a thread-safe snapshot copy of all buffered events.</summary>
    public IReadOnlyList<T> Read()
    {
        lock (_lock)
            return _buffer.Count == 0 ? [] : _buffer.ToArray();
    }

    /// <summary>Returns a snapshot copy of all events and clears the buffer.</summary>
    public IReadOnlyList<T> Drain()
    {
        lock (_lock)
        {
            if (_buffer.Count == 0)
                return [];

            var snapshot = _buffer.ToArray();
            _buffer.Clear();
            return snapshot;
        }
    }

    /// <summary>Removes all buffered events.</summary>
    public void Clear()
    {
        lock (_lock) _buffer.Clear();
    }
}

/// <summary>Write-only handle for sending events of type T.</summary>
public readonly ref struct EventWriter<T>
{
    private readonly Events<T> _events;

    public EventWriter(Events<T> events) => _events = events;

    /// <summary>Queues a single event.</summary>
    public void Send(T evt) => _events.Send(evt);

    /// <summary>Queues multiple events at once.</summary>
    public void SendBatch(ReadOnlySpan<T> events) => _events.SendBatch(events);

    /// <summary>Creates a writer from the world's event queue for T.</summary>
    public static EventWriter<T> Get(World world) => new(Events.Get<T>(world));
}

/// <summary>Read-only handle for consuming events of type T.</summary>
public readonly ref struct EventReader<T>
{
    private readonly Events<T> _events;

    public EventReader(Events<T> events) => _events = events;

    /// <summary>Number of available events.</summary>
    public int Count => _events.Count;

    /// <summary>True when there are no events to read.</summary>
    public bool IsEmpty => _events.IsEmpty;

    /// <summary>Returns a thread-safe snapshot of all buffered events.</summary>
    public IReadOnlyList<T> Read() => _events.Read();

    /// <summary>Returns a snapshot of all events and clears the buffer.</summary>
    public IReadOnlyList<T> Drain() => _events.Drain();

    /// <summary>Creates a reader from the world's event queue for T.</summary>
    public static EventReader<T> Get(World world) => new(Events.Get<T>(world));
}

/// <summary>Convenience extensions for firing and reading events directly on <see cref="World"/>.</summary>
public static class WorldEventExtensions
{
    /// <summary>Sends an event into the world's event queue for T.</summary>
    public static void SendEvent<T>(this World world, T evt)
        => Events.Get<T>(world).Send(evt);

    /// <summary>Returns a thread-safe snapshot of all buffered events of type T.</summary>
    public static IReadOnlyList<T> ReadEvents<T>(this World world)
        => Events.Get<T>(world).Read();

    /// <summary>Drains all events of type T, returning a snapshot and clearing the buffer.</summary>
    public static IReadOnlyList<T> DrainEvents<T>(this World world)
        => Events.Get<T>(world).Drain();

    /// <summary>Clears all buffered events of type T.</summary>
    public static void ClearEvents<T>(this World world)
        => Events.Get<T>(world).Clear();
}

