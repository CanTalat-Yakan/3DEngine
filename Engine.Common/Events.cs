using System.Runtime.InteropServices;

namespace Engine;

/// <summary>Static helpers to get or create typed event queues in the <see cref="World"/>.</summary>
/// <seealso cref="Events{T}"/>
/// <seealso cref="EventWriter{T}"/>
/// <seealso cref="EventReader{T}"/>
public static class Events
{
    /// <summary>Gets the event queue resource for <typeparamref name="T"/>, inserting a new one if missing.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="world">The <see cref="World"/> to look up or create the queue in.</param>
    /// <returns>The existing or newly created <see cref="Events{T}"/> queue.</returns>
    public static Events<T> Get<T>(World world)
        => world.GetOrInsertResource(() => new Events<T>());
}

/// <summary>
/// Thread-safe event queue per event type <typeparamref name="T"/>, stored as a <see cref="World"/> resource.
/// Events accumulate during a frame and should be cleared once per frame
/// (typically in the <see cref="Stage.Last"/> stage) via <see cref="Clear"/>.
/// </summary>
/// <typeparam name="T">The event payload type.</typeparam>
/// <example>
/// <code>
/// // Send an event
/// Events.Get&lt;DamageEvent&gt;(world).Send(new DamageEvent(target, 25));
///
/// // Read events (snapshot copy)
/// foreach (var evt in Events.Get&lt;DamageEvent&gt;(world).Read())
///     ApplyDamage(evt);
/// </code>
/// </example>
/// <seealso cref="Events"/>
/// <seealso cref="EventWriter{T}"/>
/// <seealso cref="EventReader{T}"/>
/// <seealso cref="WorldEventExtensions"/>
public sealed class Events<T>
{
    private readonly Lock _lock = new();
    private readonly List<T> _buffer = [];

    /// <summary>Number of buffered events.</summary>
    public int Count { get { lock (_lock) return _buffer.Count; } }

    /// <summary><c>true</c> when the buffer is empty; <c>false</c> otherwise.</summary>
    public bool IsEmpty { get { lock (_lock) return _buffer.Count == 0; } }

    /// <summary>Queues a single event into the buffer.</summary>
    /// <param name="evt">The event to enqueue.</param>
    public void Send(T evt)
    {
        lock (_lock) _buffer.Add(evt);
    }

    /// <summary>Queues multiple events at once.</summary>
    /// <param name="events">A span of events to enqueue.</param>
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
    /// </summary>
    /// <returns>A read-only span over the internal buffer.</returns>
    /// <remarks>
    /// <b>Not safe while other threads call <see cref="Send"/>.</b>
    /// Use only from stages that do not overlap with writers (e.g., a single-threaded stage
    /// or when the queue is not being written to).
    /// </remarks>
    public ReadOnlySpan<T> AsSpan() => CollectionsMarshal.AsSpan(_buffer);

    /// <summary>Returns a thread-safe snapshot copy of all buffered events.</summary>
    /// <returns>An empty list if no events are buffered; otherwise a copy of all events.</returns>
    public IReadOnlyList<T> Read()
    {
        lock (_lock)
            return _buffer.Count == 0 ? [] : _buffer.ToArray();
    }

    /// <summary>Returns a snapshot copy of all events and clears the buffer atomically.</summary>
    /// <returns>An empty list if no events are buffered; otherwise a copy of the drained events.</returns>
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

/// <summary>Write-only handle for sending events of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The event payload type.</typeparam>
/// <example>
/// <code>
/// var writer = EventWriter&lt;DamageEvent&gt;.Get(world);
/// writer.Send(new DamageEvent(target, 25));
/// </code>
/// </example>
/// <seealso cref="EventReader{T}"/>
/// <seealso cref="Events{T}"/>
public readonly ref struct EventWriter<T>
{
    private readonly Events<T> _events;

    /// <summary>Creates a writer wrapping the specified event queue.</summary>
    /// <param name="events">The underlying event queue to write into.</param>
    public EventWriter(Events<T> events) => _events = events;

    /// <summary>Queues a single event.</summary>
    /// <param name="evt">The event to enqueue.</param>
    public void Send(T evt) => _events.Send(evt);

    /// <summary>Queues multiple events at once.</summary>
    /// <param name="events">A span of events to enqueue.</param>
    public void SendBatch(ReadOnlySpan<T> events) => _events.SendBatch(events);

    /// <summary>Creates a writer from the world's event queue for <typeparamref name="T"/>.</summary>
    /// <param name="world">The <see cref="World"/> containing the event queue resource.</param>
    /// <returns>A new <see cref="EventWriter{T}"/> wrapping the queue.</returns>
    public static EventWriter<T> Get(World world) => new(Events.Get<T>(world));
}

/// <summary>Read-only handle for consuming events of type <typeparamref name="T"/>.</summary>
/// <typeparam name="T">The event payload type.</typeparam>
/// <example>
/// <code>
/// var reader = EventReader&lt;DamageEvent&gt;.Get(world);
/// foreach (var evt in reader.Read())
///     ApplyDamage(evt.Target, evt.Amount);
/// </code>
/// </example>
/// <seealso cref="EventWriter{T}"/>
/// <seealso cref="Events{T}"/>
public readonly ref struct EventReader<T>
{
    private readonly Events<T> _events;

    /// <summary>Creates a reader wrapping the specified event queue.</summary>
    /// <param name="events">The underlying event queue to read from.</param>
    public EventReader(Events<T> events) => _events = events;

    /// <summary>Number of available events in the buffer.</summary>
    public int Count => _events.Count;

    /// <summary><c>true</c> when there are no events to read.</summary>
    public bool IsEmpty => _events.IsEmpty;

    /// <summary>Returns a thread-safe snapshot of all buffered events.</summary>
    /// <returns>A read-only list of event copies.</returns>
    public IReadOnlyList<T> Read() => _events.Read();

    /// <summary>Returns a snapshot of all events and clears the buffer atomically.</summary>
    /// <returns>A read-only list of the drained events.</returns>
    public IReadOnlyList<T> Drain() => _events.Drain();

    /// <summary>Creates a reader from the world's event queue for <typeparamref name="T"/>.</summary>
    /// <param name="world">The <see cref="World"/> containing the event queue resource.</param>
    /// <returns>A new <see cref="EventReader{T}"/> wrapping the queue.</returns>
    public static EventReader<T> Get(World world) => new(Events.Get<T>(world));
}

/// <summary>Convenience extensions for firing and reading events directly on <see cref="World"/>.</summary>
/// <example>
/// <code>
/// // Send directly on World
/// world.SendEvent(new DamageEvent(target, 50));
///
/// // Read directly on World
/// foreach (var evt in world.ReadEvents&lt;DamageEvent&gt;())
///     ApplyDamage(evt);
///
/// // Drain (read + clear) at end of frame
/// var events = world.DrainEvents&lt;DamageEvent&gt;();
/// </code>
/// </example>
/// <seealso cref="Events{T}"/>
public static class WorldEventExtensions
{
    /// <summary>Sends an event into the world's event queue for <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="world">The world containing the event queue.</param>
    /// <param name="evt">The event to send.</param>
    public static void SendEvent<T>(this World world, T evt)
        => Events.Get<T>(world).Send(evt);

    /// <summary>Returns a thread-safe snapshot of all buffered events of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="world">The world containing the event queue.</param>
    /// <returns>A read-only list of event copies.</returns>
    public static IReadOnlyList<T> ReadEvents<T>(this World world)
        => Events.Get<T>(world).Read();

    /// <summary>Drains all events of type <typeparamref name="T"/>, returning a snapshot and clearing the buffer.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="world">The world containing the event queue.</param>
    /// <returns>A read-only list of the drained events.</returns>
    public static IReadOnlyList<T> DrainEvents<T>(this World world)
        => Events.Get<T>(world).Drain();

    /// <summary>Clears all buffered events of type <typeparamref name="T"/>.</summary>
    /// <typeparam name="T">The event payload type.</typeparam>
    /// <param name="world">The world containing the event queue.</param>
    public static void ClearEvents<T>(this World world)
        => Events.Get<T>(world).Clear();
}
