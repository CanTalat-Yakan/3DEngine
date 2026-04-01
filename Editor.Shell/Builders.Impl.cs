namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Concrete Builder Implementations — each populates its descriptor node.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Root builder that assembles a <see cref="ShellDescriptor"/>.</summary>
public sealed class ShellBuilder : IShellBuilder
{
    private readonly ShellDescriptor _desc = new();

    public ShellDescriptor Build() => _desc;

    public IShellBuilder MenuBar(Action<IMenuBarBuilder> configure)
    {
        var b = new MenuBarBuilder(_desc.MenuBar);
        configure(b);
        return this;
    }

    public IShellBuilder MenuBar(MenuBarMode mode, Action<IMenuBarBuilder> configure)
    {
        _desc.MenuBar.Mode = mode;
        var b = new MenuBarBuilder(_desc.MenuBar);
        configure(b);
        return this;
    }

    public IShellBuilder Toolbar(Action<IToolbarBuilder> configure)
    {
        var b = new ToolbarBuilder(_desc.Toolbar);
        configure(b);
        return this;
    }

    public IShellBuilder StatusBar(Action<IStatusBarBuilder> configure)
    {
        var b = new StatusBarBuilder(_desc.StatusBar);
        configure(b);
        return this;
    }

    public IShellBuilder StatusBar(bool inlineWithMenuBar, Action<IStatusBarBuilder> configure)
    {
        _desc.StatusBar.InlineWithMenuBar = inlineWithMenuBar;
        var b = new StatusBarBuilder(_desc.StatusBar);
        configure(b);
        return this;
    }

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

    public IShellBuilder Settings(string title, Action<ISettingsPageBuilder> configure)
    {
        var page = new SettingsPageDescriptor { Title = title };
        var b = new SettingsPageBuilder(page);
        configure(b);
        _desc.Settings.Add(page);
        return this;
    }

    public IShellBuilder Meta(string key, object value)
    {
        _desc.Metadata[key] = value;
        return this;
    }
}

// ── Menu Bar ────────────────────────────────────────────────────────────

internal sealed class MenuBarBuilder(MenuBarDescriptor desc) : IMenuBarBuilder
{
    public IMenuBarBuilder Menu(string label, Action<IMenuBuilder> configure)
    {
        var menu = new MenuDescriptor { Label = label };
        configure(new MenuBuilder(menu));
        desc.Menus.Add(menu);
        return this;
    }

    public IMenuBarBuilder Menu(string label, IconRef icon, Action<IMenuBuilder> configure)
    {
        var menu = new MenuDescriptor { Label = label, Icon = icon };
        configure(new MenuBuilder(menu));
        desc.Menus.Add(menu);
        return this;
    }
}

internal sealed class MenuBuilder(MenuDescriptor menu) : IMenuBuilder
{
    public IMenuBuilder Item(string label, Action action, string? shortcut = null, IconRef icon = default)
    {
        menu.Items.Add(new MenuItemDescriptor
        {
            Label = label,
            Action = action,
            Shortcut = shortcut,
            Icon = icon
        });
        return this;
    }

    public IMenuBuilder Item(string label, string commandId, string? shortcut = null, IconRef icon = default)
    {
        menu.Items.Add(new MenuItemDescriptor
        {
            Label = label,
            CommandId = commandId,
            Shortcut = shortcut,
            Icon = icon
        });
        return this;
    }

    public IMenuBuilder CheckItem(string label, bool isChecked, Action action, string? shortcut = null)
    {
        menu.Items.Add(new MenuItemDescriptor
        {
            Label = label,
            IsChecked = isChecked,
            Action = action,
            Shortcut = shortcut
        });
        return this;
    }

    public IMenuBuilder SubMenu(string label, Action<IMenuBuilder> configure, IconRef icon = default)
    {
        var subMenu = new MenuDescriptor { Label = label, Icon = icon };
        configure(new MenuBuilder(subMenu));
        menu.Items.Add(new MenuItemDescriptor
        {
            Label = label,
            Icon = icon,
            SubItems = subMenu.Items
        });
        return this;
    }

    public IMenuBuilder Separator()
    {
        menu.Items.Add(new MenuItemDescriptor { IsSeparator = true });
        return this;
    }
}

// ── Toolbar ─────────────────────────────────────────────────────────────

internal sealed class ToolbarBuilder(ToolbarDescriptor desc) : IToolbarBuilder
{
    public IToolbarBuilder Button(string label, Action action, IconRef icon = default, string? tooltip = null)
    {
        desc.Items.Add(new ToolbarItemDescriptor
        {
            Kind = ToolbarItemKind.Button,
            Label = label,
            Icon = icon,
            Tooltip = tooltip,
            Action = action
        });
        return this;
    }

    public IToolbarBuilder Button(string label, string commandId, IconRef icon = default, string? tooltip = null)
    {
        desc.Items.Add(new ToolbarItemDescriptor
        {
            Kind = ToolbarItemKind.Button,
            Label = label,
            Icon = icon,
            Tooltip = tooltip,
            CommandId = commandId
        });
        return this;
    }

    public IToolbarBuilder Toggle(string label, bool initial, Action<bool> onToggle, IconRef icon = default, string? tooltip = null)
    {
        desc.Items.Add(new ToolbarItemDescriptor
        {
            Kind = ToolbarItemKind.Toggle,
            Label = label,
            Icon = icon,
            Tooltip = tooltip,
            IsToggled = initial,
            ToggleAction = onToggle
        });
        return this;
    }

    public IToolbarBuilder Separator()
    {
        desc.Items.Add(new ToolbarItemDescriptor { Kind = ToolbarItemKind.Separator });
        return this;
    }

    public IToolbarBuilder Group(string groupId, Action<IToolbarBuilder> configure)
    {
        var group = new ToolbarDescriptor();
        configure(new ToolbarBuilder(group));
        desc.Items.Add(new ToolbarItemDescriptor
        {
            Kind = ToolbarItemKind.Group,
            GroupId = groupId,
            Children = group.Items
        });
        return this;
    }

    public IToolbarBuilder Dropdown(string label, Action<IMenuBuilder> configure, IconRef icon = default, string? tooltip = null)
    {
        var menu = new MenuDescriptor { Label = label };
        configure(new MenuBuilder(menu));
        desc.Items.Add(new ToolbarItemDescriptor
        {
            Kind = ToolbarItemKind.Dropdown,
            Label = label,
            Icon = icon,
            Tooltip = tooltip,
            Children = menu.Items.Select(i => new ToolbarItemDescriptor
            {
                Kind = ToolbarItemKind.Button,
                Label = i.Label,
                Icon = i.Icon,
                Action = i.Action,
                CommandId = i.CommandId
            }).ToList()
        });
        return this;
    }
}

// ── Status Bar ──────────────────────────────────────────────────────────

internal sealed class StatusBarBuilder(StatusBarDescriptor desc) : IStatusBarBuilder
{
    public IStatusBarBuilder Left(Action<IStatusBarItemBuilder> configure)
        => AddItem(StatusBarSlot.Left, configure);

    public IStatusBarBuilder Center(Action<IStatusBarItemBuilder> configure)
        => AddItem(StatusBarSlot.Center, configure);

    public IStatusBarBuilder Right(Action<IStatusBarItemBuilder> configure)
        => AddItem(StatusBarSlot.Right, configure);

    private IStatusBarBuilder AddItem(StatusBarSlot slot, Action<IStatusBarItemBuilder> configure)
    {
        var item = new StatusBarItemDescriptor { Slot = slot };
        configure(new StatusBarItemBuilder(item));
        desc.Items.Add(item);
        return this;
    }
}

internal sealed class StatusBarItemBuilder(StatusBarItemDescriptor item) : IStatusBarItemBuilder
{
    public IStatusBarItemBuilder Text(string text) { item.Text = text; return this; }
    public IStatusBarItemBuilder Binding(string expression) { item.BindingExpression = expression; return this; }
    public IStatusBarItemBuilder Icon(IconRef icon) { item.Icon = icon; return this; }
    public IStatusBarItemBuilder Widget(string widgetKey) { item.WidgetKey = widgetKey; return this; }
    public IStatusBarItemBuilder Tooltip(string tooltip) { item.Tooltip = tooltip; return this; }
    public IStatusBarItemBuilder OnClick(Action action) { item.ClickAction = action; return this; }
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
    public IPanelBuilder Icon(IconRef icon) { desc.Icon = icon; return this; }
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

// ── Settings ────────────────────────────────────────────────────────────

internal sealed class SettingsPageBuilder(SettingsPageDescriptor page) : ISettingsPageBuilder
{
    public ISettingsPageBuilder Icon(IconRef icon) { page.Icon = icon; return this; }

    public ISettingsPageBuilder Group(string title, Action<ISettingsGroupBuilder> configure)
    {
        var group = new SettingsGroupDescriptor { Title = title };
        configure(new SettingsGroupBuilder(group));
        page.Groups.Add(group);
        return this;
    }
}

internal sealed class SettingsGroupBuilder(SettingsGroupDescriptor group) : ISettingsGroupBuilder
{
    private SettingsFieldDescriptor? _lastField;

    public ISettingsGroupBuilder Field(string label, string key, FieldKind kind, object? defaultValue = null)
    {
        _lastField = new SettingsFieldDescriptor { Label = label, Key = key, Kind = kind, DefaultValue = defaultValue };
        group.Fields.Add(_lastField);
        return this;
    }

    public ISettingsGroupBuilder Field(string label, string key, FieldKind kind, float min, float max, object? defaultValue = null)
    {
        _lastField = new SettingsFieldDescriptor { Label = label, Key = key, Kind = kind, Min = min, Max = max, DefaultValue = defaultValue };
        group.Fields.Add(_lastField);
        return this;
    }

    public ISettingsGroupBuilder SliderField(string label, string key, float min, float max, float step = 0.1f, float defaultValue = 0f)
    {
        _lastField = new SettingsFieldDescriptor { Label = label, Key = key, Kind = FieldKind.Slider, Min = min, Max = max, Step = step, DefaultValue = defaultValue };
        group.Fields.Add(_lastField);
        return this;
    }

    public ISettingsGroupBuilder BoolField(string label, string key, bool defaultValue = false)
        => Field(label, key, FieldKind.Bool, defaultValue);

    public ISettingsGroupBuilder IntField(string label, string key, int defaultValue = 0, int? min = null, int? max = null)
    {
        _lastField = new SettingsFieldDescriptor { Label = label, Key = key, Kind = FieldKind.Int, DefaultValue = defaultValue, Min = min, Max = max };
        group.Fields.Add(_lastField);
        return this;
    }

    public ISettingsGroupBuilder FloatField(string label, string key, float defaultValue = 0f, float? min = null, float? max = null)
    {
        _lastField = new SettingsFieldDescriptor { Label = label, Key = key, Kind = FieldKind.Float, DefaultValue = defaultValue, Min = min, Max = max };
        group.Fields.Add(_lastField);
        return this;
    }

    public ISettingsGroupBuilder EnumField(string label, string key, string[] options, string? defaultValue = null)
    {
        _lastField = new SettingsFieldDescriptor { Label = label, Key = key, Kind = FieldKind.Enum, EnumOptions = options, DefaultValue = defaultValue };
        group.Fields.Add(_lastField);
        return this;
    }

    public ISettingsGroupBuilder Tooltip(string tooltip)
    {
        if (_lastField != null) _lastField.Tooltip = tooltip;
        return this;
    }
}
