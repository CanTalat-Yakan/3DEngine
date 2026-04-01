namespace Editor.Shell;

// ── Shell Discovery ─────────────────────────────────────────────────────

/// <summary>
/// Marks a class implementing <see cref="IEditorShellBuilder"/> for discovery
/// by the runtime script compiler. The builder's <c>Build()</c> method is called
/// to produce a <see cref="ShellDescriptor"/> tree that drives the editor UI.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class EditorShellAttribute : Attribute;

// ── Inspector Field Attributes ──────────────────────────────────────────

/// <summary>Exposes a component field in the editor inspector with an optional display label.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class FieldAttribute : Attribute
{
    public string? Label { get; }
    public FieldAttribute(string? label = null) => Label = label;
}

/// <summary>Constrains a numeric field to a [min..max] range, rendered as a slider.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class RangeAttribute : Attribute
{
    public float Min { get; }
    public float Max { get; }
    public RangeAttribute(float min, float max) { Min = min; Max = max; }
}

/// <summary>Constrains a numeric field to a minimum value.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class MinAttribute : Attribute
{
    public float Value { get; }
    public MinAttribute(float value) => Value = value;
}

/// <summary>Constrains a numeric field to a maximum value.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class MaxAttribute : Attribute
{
    public float Value { get; }
    public MaxAttribute(float value) => Value = value;
}

/// <summary>Hides a field from the inspector even if the containing type is inspectable.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class HideInInspectorAttribute : Attribute;

/// <summary>Renders a numeric field as a slider in the inspector.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class SliderAttribute : Attribute
{
    public float Step { get; }
    public SliderAttribute(float step = 0.1f) => Step = step;
}

/// <summary>Renders a color picker for Vector3/Vector4 color fields.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class ColorAttribute : Attribute;

/// <summary>Adds a tooltip to the field in the inspector.</summary>
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property, Inherited = false)]
public sealed class TooltipAttribute : Attribute
{
    public string Text { get; }
    public TooltipAttribute(string text) => Text = text;
}
