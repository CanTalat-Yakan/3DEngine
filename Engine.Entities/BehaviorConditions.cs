namespace Engine;

/// <summary>Optional modifier keys that must be held when a toggle key is pressed.</summary>
[Flags]
public enum KeyModifier
{
    None  = 0,
    Ctrl  = 1 << 0,
    Shift = 1 << 1,
    Alt   = 1 << 2,
}

/// <summary>
/// World resource that stores per-system keyboard toggle states, keyed by system descriptor name.
/// Managed automatically when <see cref="ToggleKeyAttribute"/> is used.
/// </summary>
public sealed class SystemToggleRegistry
{
    private readonly Dictionary<string, bool> _states = new();

    /// <summary>Returns the current enabled state for <paramref name="id"/>, defaulting to <paramref name="defaultEnabled"/>.</summary>
    public bool Get(string id, bool defaultEnabled = true)
        => _states.TryGetValue(id, out var v) ? v : defaultEnabled;

    /// <summary>Flips the enabled state for <paramref name="id"/>.</summary>
    public void Flip(string id, bool defaultEnabled = true)
        => _states[id] = !Get(id, defaultEnabled);
}

/// <summary>
/// Factory methods for the most common system run conditions.
/// Pass the returned <see cref="Func{World,Boolean}"/> to
/// <see cref="SystemDescriptor.RunIf"/> or use in condition methods with <see cref="RunIfAttribute"/>.
/// </summary>
/// <example>
/// Manual registration with a resource condition:
/// <code>
/// app.AddSystem(Stage.Update, MySystem,
///     BehaviorConditions.ResourceIs&lt;GameState&gt;(s => s.IsPlaying));
/// </code>
///
/// Manual registration with a keyboard toggle:
/// <code>
/// app.AddSystem(Stage.Update, MySystem,
///     BehaviorConditions.KeyToggle&lt;MyBehavior&gt;(Key.F3));
/// </code>
/// </example>
public static class BehaviorConditions
{
    // ── Resource conditions ───────────────────────────────────────────────

    /// <summary>
    /// Passes when resource <typeparamref name="T"/> is present in the world.
    /// Equivalent to Bevy's <c>resource_exists</c>.
    /// </summary>
    public static Func<World, bool> HasResource<T>() where T : notnull
        => static world => world.ContainsResource<T>();

    /// <summary>
    /// Passes when resource <typeparamref name="T"/> exists <em>and</em> satisfies <paramref name="predicate"/>.
    /// Equivalent to Bevy's <c>resource_exists_and_equals</c>.
    /// </summary>
    public static Func<World, bool> ResourceIs<T>(Func<T, bool> predicate) where T : notnull
        => world => world.TryGetResource<T>(out var r) && predicate(r);

    // ── Component / entity conditions ─────────────────────────────────────

    /// <summary>
    /// Passes when at least one entity currently has component <typeparamref name="T"/>.
    /// Equivalent to Bevy's <c>any_with_component</c>.
    /// </summary>
    public static Func<World, bool> AnyWithComponent<T>()
        => static world => world.Resource<EcsWorld>().Count<T>() > 0;

    // ── Keyboard toggle ───────────────────────────────────────────────────

    /// <summary>
    /// Returns a stateful toggle condition for manual registration.
    /// <typeparamref name="TTag"/> acts as the unique identity; use the behavior struct as the tag.
    /// </summary>
    public static Func<World, bool> KeyToggle<TTag>(Key key, KeyModifier modifier = KeyModifier.None, bool defaultEnabled = true) where TTag : notnull
        => KeyToggle(typeof(TTag).FullName!, key, modifier, defaultEnabled);

    /// <summary>
    /// String-keyed overload used by source-generated systems. Prefer the generic overload in user code.
    /// </summary>
    public static Func<World, bool> KeyToggle(string systemId, Key key, KeyModifier modifier = KeyModifier.None, bool defaultEnabled = true)
    {
        return world =>
        {
            var reg = world.GetOrInsertResource<SystemToggleRegistry>(new SystemToggleRegistry());
            if (world.TryResource<Input>() is { } input && input.KeyPressed(key) && ModifiersHeld(input, modifier))
                reg.Flip(systemId, defaultEnabled);
            return reg.Get(systemId, defaultEnabled);
        };
    }

    internal static bool ModifiersHeld(Input input, KeyModifier modifier)
    {
        if (modifier == KeyModifier.None) return true;
        if ((modifier & KeyModifier.Ctrl)  != 0 && !input.KeyDown(Key.LCtrl)  && !input.KeyDown(Key.RCtrl))  return false;
        if ((modifier & KeyModifier.Shift) != 0 && !input.KeyDown(Key.LShift) && !input.KeyDown(Key.RShift)) return false;
        if ((modifier & KeyModifier.Alt)   != 0 && !input.KeyDown(Key.LAlt)   && !input.KeyDown(Key.RAlt))   return false;
        return true;
    }
}

