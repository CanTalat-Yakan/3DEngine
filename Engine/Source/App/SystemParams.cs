namespace Engine;

/// <summary>
/// Helpers to fetch commonly used resources as system parameters, similar to Bevy system params.
/// </summary>
public readonly ref struct Res<T> where T : notnull
{
    public readonly T Value;
    public Res(T value) => Value = value;
    public static Res<T> Get(World w) => new(w.Resource<T>());
}

public readonly ref struct ResMut<T> where T : notnull
{
    public readonly T Value;
    public ResMut(T value) => Value = value;
    public static ResMut<T> Get(World w) => new(w.Resource<T>());
}

