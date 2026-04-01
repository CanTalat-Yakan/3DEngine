namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Builder Interfaces — pure C# fluent API for describing the editor layout.
//  No Blazor dependency. Scripts implement IEditorShellBuilder and use
//  IShellBuilder to declaratively register panels and metadata.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// Entry point for editor shell scripts. Classes marked with <see cref="EditorShellAttribute"/>
/// implementing this interface are discovered at runtime and their <see cref="Build"/> method
/// is called to populate the <see cref="ShellDescriptor"/>.
/// </summary>
public interface IEditorShellBuilder
{
    /// <summary>Priority for ordering multiple shell builders. Lower values run first.</summary>
    int Order => 0;

    /// <summary>Declaratively builds the editor shell layout.</summary>
    void Build(IShellBuilder shell);
}

/// <summary>Top-level shell builder. Provides access to panels and metadata.</summary>
public interface IShellBuilder
{
    /// <summary>Registers a dockable panel.</summary>
    IShellBuilder Panel(string id, string title, DockZone zone, Action<IPanelBuilder> configure);

    /// <summary>Registers a dockable panel with a widget key.</summary>
    IShellBuilder Panel(string id, string title, DockZone zone, string widgetKey);

    /// <summary>Attaches arbitrary metadata to the shell descriptor.</summary>
    IShellBuilder Meta(string key, object value);
}

// ── Panels ──────────────────────────────────────────────────────────────

public interface IPanelBuilder
{
    /// <summary>Sets the widget key for the panel content.</summary>
    IPanelBuilder Widget(string widgetKey);

    /// <summary>Builds panel content from the element content builder API.</summary>
    IPanelBuilder Content(Action<IContentBuilder> configure);

    /// <summary>Sets the icon shown in the panel tab.</summary>
    IPanelBuilder Icon(string? icon);

    /// <summary>Adds this panel as a tab in the specified tab group.</summary>
    IPanelBuilder TabGroup(string groupId, int order = 0);

    /// <summary>Sets the initial size fraction (0..1) of the parent dock area.</summary>
    IPanelBuilder InitialSize(float fraction);

    /// <summary>Whether the panel can be closed.</summary>
    IPanelBuilder Closeable(bool closeable = true);

    /// <summary>Whether the panel starts visible.</summary>
    IPanelBuilder Visible(bool visible = true);
}
