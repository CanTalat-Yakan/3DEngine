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
    Top,
    Left,
    Right,
    Bottom,
    Center,
    Float,
}

// ── Root Descriptor ─────────────────────────────────────────────────────

/// <summary>
/// Complete description of the editor shell UI.
/// Produced by <see cref="IEditorShellBuilder"/> and consumed by the Blazor renderer.
/// </summary>
public sealed class ShellDescriptor
{
    public List<PanelDescriptor> Panels { get; set; } = [];

    /// <summary>User-defined metadata.</summary>
    public Dictionary<string, object> Metadata { get; set; } = [];
}

// ── Panels (Dockable) ──────────────────────────────────────────────────

public sealed class PanelDescriptor
{
    public string Id { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Icon { get; set; }
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

    /// <summary>
    /// When set, this panel's content is served at the given URL route (e.g. "/showcase/buttons").
    /// A generic catch-all page in the host resolves the route to this panel at runtime.
    /// </summary>
    public string? Route { get; set; }
}

// ── Content-level Descriptors (used by IContentBuilder elements) ────────

/// <summary>Describes a horizontal menubar with top-level menus (rendered inside content).</summary>
public sealed class MenubarDescriptor
{
    public List<MenubarMenuDescriptor> Menus { get; set; } = [];
}

/// <summary>A single top-level menu in the menubar.</summary>
public sealed class MenubarMenuDescriptor
{
    public string Label { get; set; } = string.Empty;
    public List<Element> Items { get; set; } = [];
}

/// <summary>Describes a navigation menu with grouped links (rendered inside content).</summary>
public sealed class NavigationMenuDescriptor
{
    public List<NavMenuGroupDescriptor> Groups { get; set; } = [];
}

/// <summary>A group of navigation items, optionally with a title.</summary>
public sealed class NavMenuGroupDescriptor
{
    public string? Title { get; set; }
    public List<NavMenuItemDescriptor> Items { get; set; } = [];
}

/// <summary>A single navigation menu item with optional description and icon.</summary>
public sealed class NavMenuItemDescriptor
{
    public string Label { get; set; } = string.Empty;
    public string Href { get; set; } = "#";
    public string? Description { get; set; }
    public string? Icon { get; set; }
}

