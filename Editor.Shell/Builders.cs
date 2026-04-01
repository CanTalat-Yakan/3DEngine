namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Builder Interfaces — pure C# fluent API for describing the editor layout.
//  No Blazor dependency. Scripts implement IEditorShellBuilder and use
//  IShellBuilder to declaratively register menus, toolbars, panels, etc.
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

/// <summary>Top-level shell builder. Provides access to all editor chrome sections.</summary>
public interface IShellBuilder
{
    /// <summary>Configures the menu bar.</summary>
    IShellBuilder MenuBar(Action<IMenuBarBuilder> configure);

    /// <summary>Configures the menu bar with a specific rendering mode.</summary>
    IShellBuilder MenuBar(MenuBarMode mode, Action<IMenuBarBuilder> configure);

    /// <summary>Configures the toolbar.</summary>
    IShellBuilder Toolbar(Action<IToolbarBuilder> configure);

    /// <summary>Configures the status bar.</summary>
    IShellBuilder StatusBar(Action<IStatusBarBuilder> configure);

    /// <summary>Marks the status bar as inline with the menu bar (Rider-style).</summary>
    IShellBuilder StatusBar(bool inlineWithMenuBar, Action<IStatusBarBuilder> configure);

    /// <summary>Registers a dockable panel.</summary>
    IShellBuilder Panel(string id, string title, DockZone zone, Action<IPanelBuilder> configure);

    /// <summary>Registers a dockable panel with a widget key.</summary>
    IShellBuilder Panel(string id, string title, DockZone zone, string widgetKey);

    /// <summary>Registers a settings page accessible from the settings dialog.</summary>
    IShellBuilder Settings(string title, Action<ISettingsPageBuilder> configure);

    /// <summary>Attaches arbitrary metadata to the shell descriptor.</summary>
    IShellBuilder Meta(string key, object value);
}

// ── Menu Bar ────────────────────────────────────────────────────────────

public interface IMenuBarBuilder
{
    IMenuBarBuilder Menu(string label, Action<IMenuBuilder> configure);
    IMenuBarBuilder Menu(string label, IconRef icon, Action<IMenuBuilder> configure);
}

public interface IMenuBuilder
{
    IMenuBuilder Item(string label, Action action, string? shortcut = null, IconRef icon = default);
    IMenuBuilder Item(string label, string commandId, string? shortcut = null, IconRef icon = default);
    IMenuBuilder CheckItem(string label, bool isChecked, Action action, string? shortcut = null);
    IMenuBuilder SubMenu(string label, Action<IMenuBuilder> configure, IconRef icon = default);
    IMenuBuilder Separator();
}

// ── Toolbar ─────────────────────────────────────────────────────────────

public interface IToolbarBuilder
{
    IToolbarBuilder Button(string label, Action action, IconRef icon = default, string? tooltip = null);
    IToolbarBuilder Button(string label, string commandId, IconRef icon = default, string? tooltip = null);
    IToolbarBuilder Toggle(string label, bool initial, Action<bool> onToggle, IconRef icon = default, string? tooltip = null);
    IToolbarBuilder Separator();
    IToolbarBuilder Group(string groupId, Action<IToolbarBuilder> configure);
    IToolbarBuilder Dropdown(string label, Action<IMenuBuilder> configure, IconRef icon = default, string? tooltip = null);
}

// ── Status Bar ──────────────────────────────────────────────────────────

public interface IStatusBarBuilder
{
    IStatusBarBuilder Left(Action<IStatusBarItemBuilder> configure);
    IStatusBarBuilder Center(Action<IStatusBarItemBuilder> configure);
    IStatusBarBuilder Right(Action<IStatusBarItemBuilder> configure);
}

public interface IStatusBarItemBuilder
{
    IStatusBarItemBuilder Text(string text);
    IStatusBarItemBuilder Binding(string expression);
    IStatusBarItemBuilder Icon(IconRef icon);
    IStatusBarItemBuilder Widget(string widgetKey);
    IStatusBarItemBuilder Tooltip(string tooltip);
    IStatusBarItemBuilder OnClick(Action action);
}

// ── Panels ──────────────────────────────────────────────────────────────

public interface IPanelBuilder
{
    /// <summary>Sets the widget key for the panel content.</summary>
    IPanelBuilder Widget(string widgetKey);

    /// <summary>Builds panel content from the element content builder API.</summary>
    IPanelBuilder Content(Action<IContentBuilder> configure);

    /// <summary>Sets the icon shown in the panel tab.</summary>
    IPanelBuilder Icon(IconRef icon);

    /// <summary>Adds this panel as a tab in the specified tab group.</summary>
    IPanelBuilder TabGroup(string groupId, int order = 0);

    /// <summary>Sets the initial size fraction (0..1) of the parent dock area.</summary>
    IPanelBuilder InitialSize(float fraction);

    /// <summary>Whether the panel can be closed.</summary>
    IPanelBuilder Closeable(bool closeable = true);

    /// <summary>Whether the panel starts visible.</summary>
    IPanelBuilder Visible(bool visible = true);
}

// ── Settings ────────────────────────────────────────────────────────────

public interface ISettingsPageBuilder
{
    ISettingsPageBuilder Icon(IconRef icon);
    ISettingsPageBuilder Group(string title, Action<ISettingsGroupBuilder> configure);
}

public interface ISettingsGroupBuilder
{
    ISettingsGroupBuilder Field(string label, string key, FieldKind kind, object? defaultValue = null);
    ISettingsGroupBuilder Field(string label, string key, FieldKind kind, float min, float max, object? defaultValue = null);
    ISettingsGroupBuilder SliderField(string label, string key, float min, float max, float step = 0.1f, float defaultValue = 0f);
    ISettingsGroupBuilder BoolField(string label, string key, bool defaultValue = false);
    ISettingsGroupBuilder IntField(string label, string key, int defaultValue = 0, int? min = null, int? max = null);
    ISettingsGroupBuilder FloatField(string label, string key, float defaultValue = 0f, float? min = null, float? max = null);
    ISettingsGroupBuilder EnumField(string label, string key, string[] options, string? defaultValue = null);
    ISettingsGroupBuilder Tooltip(string tooltip);
}
