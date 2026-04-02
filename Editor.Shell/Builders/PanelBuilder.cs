namespace Editor.Shell;

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

// ═══════════════════════════════════════════════════════════════════════════
//  Builder implementations
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Root builder that assembles a <see cref="PanelDescriptor"/>.</summary>
internal sealed class PanelBuilder(PanelDescriptor desc) : IPanelBuilder
{
    public IPanelBuilder Widget(string widgetKey) { desc.WidgetKey = widgetKey; return this; }
    public IPanelBuilder Content(Action<IContentBuilder> configure)
    {
        var cb = new ContentBuilder();
        configure(cb);
        desc.Content = cb.Build();
        return this;
    }
    public IPanelBuilder Icon(string? icon) { desc.Icon = icon; return this; }
    public IPanelBuilder TabGroup(string groupId, int order = 0)
    {
        desc.TabGroupId = groupId;
        desc.TabOrder = order;
        return this;
    }
    public IPanelBuilder InitialSize(float fraction) { desc.InitialSize = Math.Clamp(fraction, 0.05f, 0.95f); return this; }
    public IPanelBuilder Closeable(bool closeable = true) { desc.Closeable = closeable; return this; }
    public IPanelBuilder Visible(bool visible = true) { desc.Visible = visible; return this; }
    public IPanelBuilder Route(string route) { desc.Route = route; return this; }
}
