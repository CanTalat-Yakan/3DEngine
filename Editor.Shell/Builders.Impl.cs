namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Concrete Builder Implementations — each populates its descriptor node.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Root builder that assembles a <see cref="ShellDescriptor"/>.</summary>
public sealed class ShellBuilder : IShellBuilder
{
    private readonly ShellDescriptor _desc = new();

    public ShellDescriptor Build() => _desc;

    public IShellBuilder Panel(string id, string title, DockZone zone, Action<IPanelBuilder> configure)
    {
        var panel = new PanelDescriptor { Id = id, Title = title, DefaultZone = zone };
        var b = new PanelBuilder(panel);
        configure(b);
        _desc.Panels.Add(panel);
        return this;
    }

    public IShellBuilder Panel(string id, string title, DockZone zone, string widgetKey)
    {
        _desc.Panels.Add(new PanelDescriptor
        {
            Id = id,
            Title = title,
            DefaultZone = zone,
            WidgetKey = widgetKey
        });
        return this;
    }

    public IShellBuilder Meta(string key, object value)
    {
        _desc.Metadata[key] = value;
        return this;
    }
}

// ── Panels ──────────────────────────────────────────────────────────────

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
}
