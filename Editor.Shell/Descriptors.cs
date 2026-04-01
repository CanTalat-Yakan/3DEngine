namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Descriptor Models — serializable POCO tree describing the entire editor UI.
//  Built by IShellBuilder implementations; consumed by the Blazor renderer.
//  Zero Blazor/ASP.NET dependencies.
// ═══════════════════════════════════════════════════════════════════════════

// ── Dock & Layout Enums ─────────────────────────────────────────────────

/// <summary>Hint for the default dock zone of a panel.</summary>
public enum DockZone
{
    Left,
    Right,
    Bottom,
    Center,
    Float,
}

/// <summary>Controls how the menu bar is rendered.</summary>
public enum MenuBarMode
{
    /// <summary>Full horizontal menu bar across the top.</summary>
    Full,
    /// <summary>Hamburger icon that opens a side sheet (Rider-style).</summary>
    Hamburger,
    /// <summary>Inline with the toolbar — menus collapse into dropdowns.</summary>
    Inline,
}

/// <summary>Horizontal alignment for status bar items.</summary>
public enum StatusBarSlot
{
    Left,
    Center,
    Right,
}

/// <summary>The kind of toolbar item.</summary>
public enum ToolbarItemKind
{
    Button,
    Toggle,
    Separator,
    Dropdown,
    Group,
    Custom,
}

/// <summary>The kind of inspector field to render.</summary>
public enum FieldKind
{
    Text,
    Int,
    Float,
    Bool,
    Vector2,
    Vector3,
    Vector4,
    Color,
    Slider,
    Enum,
    Asset,
    Custom,
}

// ── Root Descriptor ─────────────────────────────────────────────────────

/// <summary>
/// Complete description of the editor shell UI.
/// Produced by <see cref="IEditorShellBuilder"/> and consumed by the Blazor renderer.
/// </summary>
public sealed class ShellDescriptor
{
    public MenuBarDescriptor MenuBar { get; set; } = new();
    public ToolbarDescriptor Toolbar { get; set; } = new();
    public StatusBarDescriptor StatusBar { get; set; } = new();
    public List<PanelDescriptor> Panels { get; set; } = [];
    public List<SettingsPageDescriptor> Settings { get; set; } = [];

    /// <summary>User-defined metadata.</summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}

// ── Menu Bar ────────────────────────────────────────────────────────────

public sealed class MenuBarDescriptor
{
    public MenuBarMode Mode { get; set; } = MenuBarMode.Full;
    public List<MenuDescriptor> Menus { get; set; } = [];
}

public sealed class MenuDescriptor
{
    public string Label { get; set; } = string.Empty;
    public IconRef Icon { get; set; }
    public List<MenuItemDescriptor> Items { get; set; } = [];
}

public sealed class MenuItemDescriptor
{
    public string Label { get; set; } = string.Empty;
    public IconRef Icon { get; set; }
    public string? Shortcut { get; set; }
    public bool IsSeparator { get; set; }
    public bool IsDisabled { get; set; }
    public bool IsChecked { get; set; }
    public string? CommandId { get; set; }
    public Action? Action { get; set; }
    public List<MenuItemDescriptor>? SubItems { get; set; }
}

// ── Toolbar ─────────────────────────────────────────────────────────────

public sealed class ToolbarDescriptor
{
    public List<ToolbarItemDescriptor> Items { get; set; } = [];
}

public sealed class ToolbarItemDescriptor
{
    public ToolbarItemKind Kind { get; set; } = ToolbarItemKind.Button;
    public string Label { get; set; } = string.Empty;
    public IconRef Icon { get; set; }
    public string? Tooltip { get; set; }
    public bool IsToggled { get; set; }
    public string? GroupId { get; set; }
    public string? CommandId { get; set; }
    public Action? Action { get; set; }
    public Action<bool>? ToggleAction { get; set; }
    public List<ToolbarItemDescriptor>? Children { get; set; }
}

// ── Status Bar ──────────────────────────────────────────────────────────

public sealed class StatusBarDescriptor
{
    /// <summary>When true, the status bar merges into the menu bar row (Rider-style).</summary>
    public bool InlineWithMenuBar { get; set; }
    public List<StatusBarItemDescriptor> Items { get; set; } = [];
}

public sealed class StatusBarItemDescriptor
{
    public StatusBarSlot Slot { get; set; } = StatusBarSlot.Left;
    public string? Text { get; set; }
    public IconRef Icon { get; set; }
    public string? Tooltip { get; set; }
    public string? BindingExpression { get; set; }
    public string? WidgetKey { get; set; }
    public Action? ClickAction { get; set; }
}

// ── Panels (Dockable) ──────────────────────────────────────────────────

public sealed class PanelDescriptor
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public IconRef Icon { get; set; }
    public DockZone DefaultZone { get; set; } = DockZone.Left;

    /// <summary>If set, this panel is a tab inside the panel with matching <see cref="TabGroupId"/>.</summary>
    public string? TabGroupId { get; set; }
    public int TabOrder { get; set; }

    /// <summary>Key into the widget registry for the panel's content.</summary>
    public string WidgetKey { get; set; } = string.Empty;

    /// <summary>Element tree built from the content builder API. Takes priority over WidgetKey.</summary>
    public List<Element>? Content { get; set; }

    /// <summary>Initial size as a fraction (0..1) of the parent dock area.</summary>
    public float InitialSize { get; set; } = 0.25f;

    /// <summary>Whether the panel can be closed by the user.</summary>
    public bool Closeable { get; set; } = true;

    /// <summary>Whether the panel is visible on creation.</summary>
    public bool Visible { get; set; } = true;
}

// ── Settings ────────────────────────────────────────────────────────────

public sealed class SettingsPageDescriptor
{
    public string Title { get; set; } = string.Empty;
    public IconRef Icon { get; set; }
    public List<SettingsGroupDescriptor> Groups { get; set; } = [];
}

public sealed class SettingsGroupDescriptor
{
    public string Title { get; set; } = string.Empty;
    public List<SettingsFieldDescriptor> Fields { get; set; } = [];
}

public sealed class SettingsFieldDescriptor
{
    public string Label { get; set; } = string.Empty;
    public string Key { get; set; } = string.Empty;
    public FieldKind Kind { get; set; } = FieldKind.Text;
    public object? DefaultValue { get; set; }
    public float? Min { get; set; }
    public float? Max { get; set; }
    public float? Step { get; set; }
    public string? Tooltip { get; set; }
    public string[]? EnumOptions { get; set; }
}

// ── Component Inspector ─────────────────────────────────────────────────

/// <summary>
/// Describes the inspectable fields of a component type.
/// Produced by the runtime compiler from [Field] / [Range] / etc. attributes.
/// </summary>
public sealed class ComponentFieldDescriptor
{
    public string FieldName { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string TypeName { get; set; } = string.Empty;
    public FieldKind Kind { get; set; } = FieldKind.Text;
    public float? Min { get; set; }
    public float? Max { get; set; }
    public float? Step { get; set; }
    public bool Hidden { get; set; }
    public bool IsColor { get; set; }
    public string? Tooltip { get; set; }
}

/// <summary>Describes an inspectable component type and all its fields.</summary>
public sealed class InspectableComponentDescriptor
{
    public string TypeName { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<ComponentFieldDescriptor> Fields { get; set; } = [];
}
