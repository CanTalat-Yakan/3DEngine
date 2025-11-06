namespace Engine;

public static class Events
{
    public static Events<T> Get<T>(World w)
    {
        var ev = w.TryResource<Events<T>>();
        if (ev is null)
        {
            ev = new Events<T>();
            w.InsertResource(ev);
        }
        return ev;
    }
}

public readonly ref struct EvWriter<T>
{
    private readonly Events<T> _ev;
    public EvWriter(Events<T> ev) => _ev = ev;
    public void Send(T evt) => _ev.Send(evt);
    public static EvWriter<T> Get(World w) => new(Events.Get<T>(w));
}

public readonly ref struct EvReader<T>
{
    private readonly Events<T> _ev;
    public EvReader(Events<T> ev) => _ev = ev;
    public IEnumerable<T> Drain() => _ev.Drain();
    public static EvReader<T> Get(World w) => new(Events.Get<T>(w));
}
