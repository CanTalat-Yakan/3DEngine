namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Strongly-typed component variant enums - no Blazor dependency.
//  Mapped to BlazorBlueprint variant types by the rendering layer.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Visual style variants for buttons.</summary>
public enum ButtonStyle
{
    Default,
    Secondary,
    Outline,
    Ghost,
    Destructive,
    Link
}

/// <summary>Visual style variants for badges.</summary>
public enum BadgeStyle
{
    Default,
    Secondary,
    Outline,
    Destructive
}

/// <summary>Visual style variants for alerts.</summary>
public enum AlertStyle
{
    Default,
    Danger,
    Info,
    Success,
    Warning
}

/// <summary>Visual style variants for toggles.</summary>
public enum ToggleStyle
{
    Default,
    Outline
}

/// <summary>Visual style variants for toasts.</summary>
public enum ToastStyle
{
    Default,
    Destructive
}

/// <summary>Tooltip / Popover / HoverCard placement side.</summary>
public enum Side
{
    Top,
    Bottom,
    Left,
    Right
}

/// <summary>
/// Strongly-typed variant resolver. Converts variant enums to their lowercase string names
/// used by the rendering layer.
/// <para>Usage: <c>Variant.From(ButtonStyle.Ghost)</c> or <c>Variant.None</c></para>
/// </summary>
public static class Variant
{
    /// <summary>No variant (uses the component default).</summary>
    public const string? None = null;

    /// <summary>Resolve a <see cref="ButtonStyle"/> to its string name.</summary>
    public static string From(ButtonStyle style) => style.ToVariantName();

    /// <summary>Resolve a <see cref="BadgeStyle"/> to its string name.</summary>
    public static string From(BadgeStyle style) => style.ToVariantName();

    /// <summary>Resolve a <see cref="AlertStyle"/> to its string name.</summary>
    public static string From(AlertStyle style) => style.ToVariantName();

    /// <summary>Resolve a <see cref="ToggleStyle"/> to its string name.</summary>
    public static string From(ToggleStyle style) => style.ToVariantName();

    /// <summary>Resolve a <see cref="ToastStyle"/> to its string name.</summary>
    public static string From(ToastStyle style) => style.ToVariantName();

    /// <summary>Resolve a <see cref="Side"/> to its string name.</summary>
    public static string From(Side side) => side.ToVariantName();

    /// <summary>Pass through a raw variant name string.</summary>
    public static string Custom(string name) => name;
}

/// <summary>Extension methods for converting variant enums to their string representation.</summary>
public static class VariantExtensions
{
    public static string ToVariantName(this ButtonStyle style) => style switch
    {
        ButtonStyle.Secondary => "secondary",
        ButtonStyle.Outline => "outline",
        ButtonStyle.Ghost => "ghost",
        ButtonStyle.Destructive => "destructive",
        ButtonStyle.Link => "link",
        _ => "default"
    };

    public static string ToVariantName(this BadgeStyle style) => style switch
    {
        BadgeStyle.Secondary => "secondary",
        BadgeStyle.Outline => "outline",
        BadgeStyle.Destructive => "destructive",
        _ => "default"
    };

    public static string ToVariantName(this AlertStyle style) => style switch
    {
        AlertStyle.Danger => "danger",
        AlertStyle.Info => "info",
        AlertStyle.Success => "success",
        AlertStyle.Warning => "warning",
        _ => "default"
    };

    public static string ToVariantName(this ToggleStyle style) => style switch
    {
        ToggleStyle.Outline => "outline",
        _ => "default"
    };

    public static string ToVariantName(this ToastStyle style) => style switch
    {
        ToastStyle.Destructive => "destructive",
        _ => "default"
    };

    public static string ToVariantName(this Side side) => side switch
    {
        Side.Top => "top",
        Side.Bottom => "bottom",
        Side.Left => "left",
        Side.Right => "right",
        _ => "top"
    };
}
