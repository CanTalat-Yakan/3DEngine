namespace Engine;

/// <summary>Marks a method for registration by the generator; must be used on methods in a struct with <see cref="BehaviorAttribute"/>.</summary>
[AttributeUsage(AttributeTargets.Method, AllowMultiple = true, Inherited = false)]
public sealed class GeneratedBehaviorRegistrationAttribute : Attribute;

/// <summary>Marks a struct as an ECS Behavior; methods with stage attributes will be scheduled by the generator.</summary>
[AttributeUsage(AttributeTargets.Struct, Inherited = false, AllowMultiple = false)]
public sealed class BehaviorAttribute : Attribute;

/// <summary>Runs once during app startup, before the window loop begins.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnStartupAttribute : Attribute;

/// <summary>Runs at the beginning of each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnFirstAttribute : Attribute;

/// <summary>Runs before Update each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnPreUpdateAttribute : Attribute;

/// <summary>Runs during the main update stage each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnUpdateAttribute : Attribute;

/// <summary>Runs after Update each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnPostUpdateAttribute : Attribute;

/// <summary>Runs during the render stage each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnRenderAttribute : Attribute;

/// <summary>Runs at the very end of each frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnLastAttribute : Attribute;

/// <summary>Runs once during app cleanup, after the window loop ends.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class OnCleanupAttribute : Attribute;

/// <summary>Filter: schedule only for entities that also have all listed component types.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class WithAttribute : Attribute
{
    public Type[] Types { get; }
    public WithAttribute(params Type[] types) => Types = types;
}

/// <summary>Filter: skip entities that have any of the listed component types.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class WithoutAttribute : Attribute
{
    public Type[] Types { get; }
    public WithoutAttribute(params Type[] types) => Types = types;
}

/// <summary>Filter: run only if any of the listed component types changed this frame.</summary>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ChangedAttribute : Attribute
{
    public Type[] Types { get; }
    public ChangedAttribute(params Type[] types) => Types = types;
}

/// <summary>
/// Attaches a run condition to a behavior system method.
/// <paramref name="methodName"/> must be a <c>static bool(Engine.World)</c> method declared on the
/// same behavior struct. Use <c>nameof(...)</c> to keep the reference refactor-safe.
/// The system is skipped for the frame when the condition returns <c>false</c>.
/// </summary>
/// <example>
/// <code>
/// [OnUpdate]
/// [RunIf(nameof(IsGamePlaying))]
/// public static void Tick(BehaviorContext ctx) { ... }
///
/// public static bool IsGamePlaying(World world)
///     => world.TryGetResource&lt;GameState&gt;(out var s) &amp;&amp; s.IsPlaying;
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class RunIfAttribute : Attribute
{
    /// <summary>Name of the <c>static bool(World)</c> condition method on the same behavior struct.</summary>
    public string MethodName { get; }
    public RunIfAttribute(string methodName) => MethodName = methodName;
}

/// <summary>
/// Binds a keyboard shortcut directly to a system, toggling it on/off without any boilerplate method.
/// Each press of <paramref name="key"/> (with optional <paramref name="modifier"/> held) flips the
/// enabled state. The system starts enabled unless <c>DefaultEnabled = false</c> is set.
/// </summary>
/// <remarks>
/// Toggle state is managed via <see cref="SystemToggleRegistry"/> in <see cref="BehaviorConditions"/>.
/// </remarks>
/// <example>
/// <code>
/// [OnRender]
/// [ToggleKey(Key.F3)]                          // F3 alone
/// [ToggleKey(Key.F3, DefaultEnabled = false)]  // F3, default off
/// [ToggleKey(Key.F3, KeyModifier.Ctrl)]        // Ctrl + F3
/// public static void Draw(BehaviorContext ctx) { ... }
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Method, Inherited = false, AllowMultiple = false)]
public sealed class ToggleKeyAttribute : Attribute
{
    public Key Key { get; }
    public KeyModifier Modifier { get; }
    public bool DefaultEnabled { get; init; } = true;
    public ToggleKeyAttribute(Key key, KeyModifier modifier = KeyModifier.None)
    {
        Key = key;
        Modifier = modifier;
    }
}


