namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Concrete Content Builder — builds Element trees from fluent API calls.
// ═══════════════════════════════════════════════════════════════════════════

public sealed class ContentBuilder : IContentBuilder
{
    private readonly List<Element> _elements = [];
    public List<Element> Build() => _elements;

    // ── Text & Display ──────────────────────────────────────────────────

    public IContentBuilder Text(string? css, string text)
    { _elements.Add(new Element("text") { Text = text, Css = css }); return this; }

    public IContentBuilder Heading(string? css, int level, string text)
    { _elements.Add(new Element($"h{Math.Clamp(level, 1, 6)}") { Text = text, Css = css }); return this; }

    public IContentBuilder Paragraph(string? css, string text)
    { _elements.Add(new Element("p") { Text = text, Css = css }); return this; }

    public IContentBuilder Label(string? css, string text, string? forId = null)
    { _elements.Add(new Element("label") { Text = text, Id = forId, Css = css }); return this; }

    public IContentBuilder Icon(string? css, string? icon, int size = 16)
    { _elements.Add(new Element("icon") { Text = icon, Css = css, Props = { ["size"] = size, ["icon"] = icon } }); return this; }

    public IContentBuilder Badge(string? css, string text, string? variant = null)
    { _elements.Add(new Element("badge") { Text = text, Css = css, Props = { ["variant"] = variant } }); return this; }

    public IContentBuilder Code(string? css, string text)
    { _elements.Add(new Element("code") { Text = text, Css = css }); return this; }

    public IContentBuilder Avatar(string? css, string? fallback = null, string? src = null)
    { _elements.Add(new Element("avatar") { Text = fallback, Css = css, Props = { ["src"] = src } }); return this; }

    public IContentBuilder Progress(string? css, int value, int max = 100)
    { _elements.Add(new Element("progress") { Css = css, Props = { ["value"] = value, ["max"] = max } }); return this; }

    public IContentBuilder Skeleton(string? css, string? width = null, string? height = null)
    { _elements.Add(new Element("skeleton") { Css = css, Props = { ["width"] = width, ["height"] = height } }); return this; }

    public IContentBuilder Spinner(string? css, int size = 16)
    { _elements.Add(new Element("spinner") { Css = css, Props = { ["size"] = size } }); return this; }

    public IContentBuilder Kbd(string? css, string keys)
    { _elements.Add(new Element("kbd") { Text = keys, Css = css }); return this; }

    // ── Interactive ─────────────────────────────────────────────────────

    public IContentBuilder Button(string? css, string label, Action? onClick = null, string? variant = null, string? icon = null,
        bool disabled = false, bool loading = false, string? href = null)
    {
        _elements.Add(new Element("button") { Text = label, OnClick = onClick, Css = css,
            Props = { ["variant"] = variant, ["icon"] = icon, ["disabled"] = disabled, ["loading"] = loading, ["href"] = href } });
        return this;
    }

    public IContentBuilder Input(string? css, string? placeholder = null, string? value = null, Action<string>? onChanged = null, string? id = null)
    {
        _elements.Add(new Element("input") { Id = id, Css = css, OnInput = onChanged,
            Props = { ["placeholder"] = placeholder, ["value"] = value } });
        return this;
    }

    public IContentBuilder Textarea(string? css, string? placeholder = null, string? value = null, Action<string>? onChanged = null, string? id = null)
    {
        _elements.Add(new Element("textarea") { Id = id, Css = css, OnInput = onChanged,
            Props = { ["placeholder"] = placeholder, ["value"] = value } });
        return this;
    }

    public IContentBuilder Checkbox(string? css, string? label = null, bool initial = false, Action<bool>? onChanged = null, string? id = null)
    {
        _elements.Add(new Element("checkbox") { Text = label, Id = id, Css = css, OnToggle = onChanged,
            Props = { ["checked"] = initial } });
        return this;
    }

    public IContentBuilder Switch(string? css, string? label = null, bool initial = false, Action<bool>? onChanged = null, string? id = null)
    {
        _elements.Add(new Element("switch") { Text = label, Id = id, Css = css, OnToggle = onChanged,
            Props = { ["checked"] = initial } });
        return this;
    }

    public IContentBuilder Select(string? css, (string Value, string Label)[] options, string? placeholder = null, string? selected = null,
        Action<string>? onChanged = null, string? id = null)
    {
        _elements.Add(new Element("select") { Id = id, Css = css, OnInput = onChanged,
            Props = { ["placeholder"] = placeholder, ["selected"] = selected, ["options"] = options } });
        return this;
    }

    public IContentBuilder RadioGroup(string? css, (string Value, string Label)[] options, string? selected = null,
        Action<string>? onChanged = null)
    {
        _elements.Add(new Element("radio-group") { Css = css, OnInput = onChanged,
            Props = { ["selected"] = selected, ["options"] = options } });
        return this;
    }

    public IContentBuilder Slider(string? css, double value = 0, double min = 0, double max = 100, double step = 1,
        Action<double>? onChanged = null)
    {
        _elements.Add(new Element("slider") { Css = css, OnValueChanged = onChanged,
            Props = { ["value"] = value, ["min"] = min, ["max"] = max, ["step"] = step } });
        return this;
    }

    public IContentBuilder NumericInput(string? css, double value = 0, double? min = null, double? max = null, double step = 1,
        Action<double>? onChanged = null, string? id = null)
    {
        _elements.Add(new Element("numeric-input") { Id = id, Css = css, OnValueChanged = onChanged,
            Props = { ["value"] = value, ["min"] = min, ["max"] = max, ["step"] = step } });
        return this;
    }

    public IContentBuilder Combobox(string? css, (string Value, string Label)[] options, string? placeholder = null,
        string? selected = null, Action<string>? onChanged = null, string? id = null)
    {
        _elements.Add(new Element("combobox") { Id = id, Css = css, OnInput = onChanged,
            Props = { ["placeholder"] = placeholder, ["selected"] = selected, ["options"] = options } });
        return this;
    }

    public IContentBuilder Toggle(string? css, string? label = null, bool pressed = false, Action<bool>? onChanged = null,
        string? variant = null, string? icon = null)
    {
        _elements.Add(new Element("toggle") { Text = label, Css = css, OnToggle = onChanged,
            Props = { ["pressed"] = pressed, ["variant"] = variant, ["icon"] = icon } });
        return this;
    }

    public IContentBuilder ToggleGroup(string? css, (string Value, string Label)[] options, string? selected = null,
        Action<string>? onChanged = null, string? variant = null)
    {
        _elements.Add(new Element("toggle-group") { Css = css, OnInput = onChanged,
            Props = { ["selected"] = selected, ["options"] = options, ["variant"] = variant } });
        return this;
    }

    // ── Layout ──────────────────────────────────────────────────────────

    public IContentBuilder Separator(string? css = null)
    { _elements.Add(new Element("separator") { Css = css }); return this; }

    public IContentBuilder Spacer()
    { _elements.Add(new Element("spacer")); return this; }

    public IContentBuilder Div(string? css, Action<IContentBuilder> children)
    { _elements.Add(BuildContainer("div", css, children)); return this; }

    public IContentBuilder Row(string? css, Action<IContentBuilder> children)
    { _elements.Add(BuildContainer("row", css, children)); return this; }

    public IContentBuilder Column(string? css, Action<IContentBuilder> children)
    { _elements.Add(BuildContainer("column", css, children)); return this; }

    public IContentBuilder Grid(string? css, int columns, Action<IContentBuilder> children)
    {
        var el = BuildContainer("grid", css, children);
        el.Props["columns"] = columns;
        _elements.Add(el);
        return this;
    }

    public IContentBuilder ScrollArea(string? css, Action<IContentBuilder> children)
    { _elements.Add(BuildContainer("scroll", css, children)); return this; }

    public IContentBuilder Accordion(string? css, Action<IAccordionBuilder> configure, string? type = null)
    {
        var accordion = new Element("accordion") { Css = css, Props = { ["type"] = type } };
        var builder = new AccordionBuilder(accordion);
        configure(builder);
        _elements.Add(accordion);
        return this;
    }

    public IContentBuilder AspectRatio(string? css, double ratio, Action<IContentBuilder> content)
    {
        var el = BuildContainer("aspect-ratio", css, content);
        el.Props["ratio"] = ratio;
        _elements.Add(el);
        return this;
    }

    // ── Cards ───────────────────────────────────────────────────────────

    public IContentBuilder Card(string? css, Action<ICardBuilder> configure)
    {
        var card = new Element("card") { Css = css };
        var builder = new CardBuilder(card);
        configure(builder);
        _elements.Add(card);
        return this;
    }

    // ── Feedback ────────────────────────────────────────────────────────

    public IContentBuilder Alert(string? css, string? title = null, string? description = null, string? variant = null,
        string? icon = null)
    {
        var el = new Element("alert") { Css = css, Props = { ["variant"] = variant, ["icon"] = icon } };
        if (title != null) el.Children.Add(new Element("alert-title") { Text = title });
        if (description != null) el.Children.Add(new Element("alert-description") { Text = description });
        _elements.Add(el);
        return this;
    }

    public IContentBuilder Toast(string? css, string title, string? description = null, string? variant = null,
        string? icon = null)
    {
        _elements.Add(new Element("toast") { Css = css,
            Props = { ["title"] = title, ["description"] = description, ["variant"] = variant, ["icon"] = icon } });
        return this;
    }

    // ── Links ───────────────────────────────────────────────────────────

    public IContentBuilder Link(string? css, string text, string href, string? icon = null, string? description = null)
    {
        _elements.Add(new Element("link") { Text = text, Css = css,
            Props = { ["href"] = href, ["icon"] = icon, ["description"] = description } });
        return this;
    }

    // ── Navigation ──────────────────────────────────────────────────────

    public IContentBuilder Breadcrumb(string? css, Action<IBreadcrumbBuilder> configure)
    {
        var breadcrumb = new Element("breadcrumb") { Css = css };
        var builder = new BreadcrumbBuilder(breadcrumb);
        configure(builder);
        _elements.Add(breadcrumb);
        return this;
    }

    public IContentBuilder Pagination(string? css, int currentPage, int totalPages, Action<int>? onPageChanged = null)
    {
        _elements.Add(new Element("pagination") { Css = css, OnIndexChanged = onPageChanged,
            Props = { ["currentPage"] = currentPage, ["totalPages"] = totalPages } });
        return this;
    }

    // ── Overlays & Popups ───────────────────────────────────────────────

    public IContentBuilder Tooltip(string? css, Action<IContentBuilder> trigger, string tip, string? side = null)
    {
        var triggerEl = BuildChildren(trigger);
        _elements.Add(new Element("tooltip") { Text = tip, Css = css,
            Props = { ["side"] = side, ["trigger"] = triggerEl } });
        return this;
    }

    public IContentBuilder DropdownMenu(string? css, string triggerLabel, Action<IDropdownMenuBuilder> configure,
        string? triggerVariant = null, string? triggerIcon = null)
    {
        var menu = new Element("dropdown-menu") { Text = triggerLabel, Css = css,
            Props = { ["triggerVariant"] = triggerVariant, ["triggerIcon"] = triggerIcon } };
        var builder = new DropdownMenuBuilder(menu);
        configure(builder);
        _elements.Add(menu);
        return this;
    }

    public IContentBuilder ContextMenu(string? css, Action<IContentBuilder> trigger, Action<IDropdownMenuBuilder> configure)
    {
        var triggerEl = BuildChildren(trigger);
        var menu = new Element("context-menu") { Css = css, Props = { ["trigger"] = triggerEl } };
        var builder = new DropdownMenuBuilder(menu);
        configure(builder);
        _elements.Add(menu);
        return this;
    }

    public IContentBuilder Popover(string? css, Action<IContentBuilder> trigger, Action<IContentBuilder> content)
    {
        var triggerEl = BuildChildren(trigger);
        var contentEl = BuildChildren(content);
        _elements.Add(new Element("popover") { Css = css,
            Props = { ["trigger"] = triggerEl, ["content"] = contentEl } });
        return this;
    }

    public IContentBuilder HoverCard(string? css, Action<IContentBuilder> trigger, Action<IContentBuilder> content)
    {
        var triggerEl = BuildChildren(trigger);
        var contentEl = BuildChildren(content);
        _elements.Add(new Element("hover-card") { Css = css,
            Props = { ["trigger"] = triggerEl, ["content"] = contentEl } });
        return this;
    }

    public IContentBuilder Drawer(string triggerLabel, Action<IDrawerBuilder> configure, string? direction = null,
        string? triggerVariant = null)
    {
        var drawer = new Element("drawer") {
            Props = { ["triggerLabel"] = triggerLabel, ["triggerVariant"] = triggerVariant,
                       ["direction"] = direction, ["open"] = false } };
        var builder = new DrawerBuilder(drawer);
        configure(builder);
        _elements.Add(drawer);
        return this;
    }

    public IContentBuilder Sheet(string triggerLabel, Action<ISheetBuilder> configure, string? side = null,
        string? triggerVariant = null)
    {
        var sheet = new Element("sheet") {
            Props = { ["triggerLabel"] = triggerLabel, ["triggerVariant"] = triggerVariant,
                       ["side"] = side, ["open"] = false } };
        var builder = new SheetBuilder(sheet);
        configure(builder);
        _elements.Add(sheet);
        return this;
    }

    // ── Complex Components ──────────────────────────────────────────────

    public IContentBuilder Tabs(string? css, Action<ITabsBuilder> configure)
    {
        var tabs = new Element("tabs") { Css = css };
        var builder = new TabsBuilder(tabs);
        configure(builder);
        _elements.Add(tabs);
        return this;
    }

    public IContentBuilder Collapsible(string? css, string title, Action<IContentBuilder> content, bool expanded = false)
    {
        var cb = new ContentBuilder();
        content(cb);
        _elements.Add(new Element("collapsible") { Text = title, Css = css,
            Props = { ["expanded"] = expanded }, Children = cb.Build() });
        return this;
    }

    public IContentBuilder Dialog(string triggerLabel, Action<IDialogBuilder> configure, string? triggerVariant = null)
    {
        var dialog = new Element("dialog") { Props = { ["triggerLabel"] = triggerLabel, ["triggerVariant"] = triggerVariant, ["open"] = false } };
        var builder = new DialogBuilder(dialog);
        configure(builder);
        _elements.Add(dialog);
        return this;
    }

    public IContentBuilder AlertDialog(string triggerLabel, Action<IAlertDialogBuilder> configure, string? triggerVariant = null)
    {
        var dialog = new Element("alert-dialog") { Props = { ["triggerLabel"] = triggerLabel, ["triggerVariant"] = triggerVariant, ["open"] = false } };
        var builder = new AlertDialogBuilder(dialog);
        configure(builder);
        _elements.Add(dialog);
        return this;
    }

    // ── Editor-specific ─────────────────────────────────────────────────

    public IContentBuilder Menubar(string? css, Action<IMenubarBuilder> configure)
    {
        var builder = new MenubarBuilder();
        configure(builder);
        var el = new Element("menubar") { Css = css, Props = { ["descriptor"] = builder.Descriptor } };
        _elements.Add(el);
        return this;
    }

    public IContentBuilder NavigationMenu(string? css, Action<INavigationMenuBuilder> configure)
    {
        var builder = new NavigationMenuBuilder();
        configure(builder);
        var el = new Element("navigation-menu") { Css = css, Props = { ["descriptor"] = builder.Descriptor } };
        _elements.Add(el);
        return this;
    }

    public IContentBuilder TreeItem(string label, string? icon = null, bool selected = false, bool expanded = true,
        Action? onClick = null, Action<IContentBuilder>? children = null, string? iconColor = null)
    {
        var el = new Element("tree-item") { Text = label, OnClick = onClick,
            Props = { ["icon"] = icon, ["selected"] = selected, ["expanded"] = expanded, ["iconColor"] = iconColor } };
        if (children != null)
        {
            var cb = new ContentBuilder();
            children(cb);
            el.Children = cb.Build();
        }
        _elements.Add(el);
        return this;
    }

    public IContentBuilder FieldRow(string label, Action<IContentBuilder> control)
    {
        var el = new Element("field-row") { Text = label };
        var cb = new ContentBuilder();
        control(cb);
        el.Children = cb.Build();
        _elements.Add(el);
        return this;
    }

    public IContentBuilder EmptyState(string? icon = null, string? title = null, string? description = null)
    {
        _elements.Add(new Element("empty-state") { Props = { ["icon"] = icon, ["title"] = title, ["description"] = description } });
        return this;
    }

    public IContentBuilder LogEntry(string time, string level, string category, string message)
    {
        _elements.Add(new Element("log-entry") { Props = { ["time"] = time, ["level"] = level, ["category"] = category, ["message"] = message } });
        return this;
    }

    public IContentBuilder DarkModeToggle(string? css = null)
    {
        _elements.Add(new Element("dark-mode-toggle") { Css = css });
        return this;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static Element BuildContainer(string tag, string? css, Action<IContentBuilder> children)
    {
        var cb = new ContentBuilder();
        children(cb);
        return new Element(tag) { Css = css, Children = cb.Build() };
    }

    private static List<Element> BuildChildren(Action<IContentBuilder> configure)
    {
        var cb = new ContentBuilder();
        configure(cb);
        return cb.Build();
    }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Sub-builder implementations
// ═══════════════════════════════════════════════════════════════════════════

// ── Card Builder ────────────────────────────────────────────────────────

internal sealed class CardBuilder(Element card) : ICardBuilder
{
    private Element? _header;

    private Element EnsureHeader()
    {
        if (_header != null) return _header;
        _header = new Element("card-header");
        // Insert header at position 0
        card.Children.Insert(0, _header);
        return _header;
    }

    public ICardBuilder Title(string text)
    { EnsureHeader().Children.Add(new Element("card-title") { Text = text }); return this; }

    public ICardBuilder Description(string text)
    { EnsureHeader().Children.Add(new Element("card-description") { Text = text }); return this; }

    public ICardBuilder Header(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        var h = EnsureHeader();
        h.Children.AddRange(cb.Build());
        return this;
    }

    public ICardBuilder Content(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        card.Children.Add(new Element("card-content") { Children = cb.Build() });
        return this;
    }

    public ICardBuilder Footer(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        card.Children.Add(new Element("card-footer") { Children = cb.Build() });
        return this;
    }

    public ICardBuilder Css(string css) { card.Css = css; return this; }
}

// ── Tabs Builder ────────────────────────────────────────────────────────

internal sealed class TabsBuilder(Element tabs) : ITabsBuilder
{
    public ITabsBuilder Tab(string label, Action<IContentBuilder> content, string? icon = null)
    {
        var cb = new ContentBuilder();
        content(cb);
        tabs.Children.Add(new Element("tab") { Text = label, Children = cb.Build(),
            Props = { ["icon"] = icon } });
        return this;
    }
}

// ── Dialog Builder ──────────────────────────────────────────────────────

internal sealed class DialogBuilder(Element dialog) : IDialogBuilder
{
    public IDialogBuilder Title(string text)
    { dialog.Children.Add(new Element("dialog-title") { Text = text }); return this; }

    public IDialogBuilder Description(string text)
    { dialog.Children.Add(new Element("dialog-description") { Text = text }); return this; }

    public IDialogBuilder Content(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        dialog.Children.Add(new Element("dialog-content") { Children = cb.Build() });
        return this;
    }

    public IDialogBuilder Footer(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        dialog.Children.Add(new Element("dialog-footer") { Children = cb.Build() });
        return this;
    }
}

// ── Alert Dialog Builder ────────────────────────────────────────────────

internal sealed class AlertDialogBuilder(Element dialog) : IAlertDialogBuilder
{
    public IAlertDialogBuilder Title(string text)
    { dialog.Children.Add(new Element("alert-dialog-title") { Text = text }); return this; }

    public IAlertDialogBuilder Description(string text)
    { dialog.Children.Add(new Element("alert-dialog-description") { Text = text }); return this; }

    public IAlertDialogBuilder CancelText(string text)
    { dialog.Props["cancelText"] = text; return this; }

    public IAlertDialogBuilder ConfirmText(string text)
    { dialog.Props["confirmText"] = text; return this; }

    public IAlertDialogBuilder OnConfirm(Action action)
    { dialog.OnClick = action; return this; }
}

// ── Accordion Builder ───────────────────────────────────────────────────

internal sealed class AccordionBuilder(Element accordion) : IAccordionBuilder
{
    public IAccordionBuilder Item(string value, string title, Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        accordion.Children.Add(new Element("accordion-item") { Text = title, Children = cb.Build(),
            Props = { ["value"] = value } });
        return this;
    }
}

// ── Dropdown Menu Builder ───────────────────────────────────────────────

internal sealed class DropdownMenuBuilder(Element menu) : IDropdownMenuBuilder
{
    public IDropdownMenuBuilder Item(string label, Action? onClick = null, string? icon = null,
        string? shortcut = null, bool disabled = false)
    {
        menu.Children.Add(new Element("menu-item") { Text = label, OnClick = onClick,
            Props = { ["icon"] = icon, ["shortcut"] = shortcut, ["disabled"] = disabled } });
        return this;
    }

    public IDropdownMenuBuilder CheckboxItem(string label, bool initial = false, Action<bool>? onChanged = null)
    {
        menu.Children.Add(new Element("menu-checkbox-item") { Text = label, OnToggle = onChanged,
            Props = { ["checked"] = initial } });
        return this;
    }

    public IDropdownMenuBuilder Separator()
    {
        menu.Children.Add(new Element("menu-separator"));
        return this;
    }

    public IDropdownMenuBuilder Label(string text)
    {
        menu.Children.Add(new Element("menu-label") { Text = text });
        return this;
    }

    public IDropdownMenuBuilder Sub(string label, Action<IDropdownMenuBuilder> submenu, string? icon = null)
    {
        var sub = new Element("menu-sub") { Text = label, Props = { ["icon"] = icon } };
        var builder = new DropdownMenuBuilder(sub);
        submenu(builder);
        menu.Children.Add(sub);
        return this;
    }
}

// ── Drawer Builder ──────────────────────────────────────────────────────

internal sealed class DrawerBuilder(Element drawer) : IDrawerBuilder
{
    public IDrawerBuilder Title(string text)
    { drawer.Children.Add(new Element("drawer-title") { Text = text }); return this; }

    public IDrawerBuilder Description(string text)
    { drawer.Children.Add(new Element("drawer-description") { Text = text }); return this; }

    public IDrawerBuilder Content(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        drawer.Children.Add(new Element("drawer-content") { Children = cb.Build() });
        return this;
    }

    public IDrawerBuilder Footer(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        drawer.Children.Add(new Element("drawer-footer") { Children = cb.Build() });
        return this;
    }
}

// ── Sheet Builder ───────────────────────────────────────────────────────

internal sealed class SheetBuilder(Element sheet) : ISheetBuilder
{
    public ISheetBuilder Title(string text)
    { sheet.Children.Add(new Element("sheet-title") { Text = text }); return this; }

    public ISheetBuilder Description(string text)
    { sheet.Children.Add(new Element("sheet-description") { Text = text }); return this; }

    public ISheetBuilder Content(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        sheet.Children.Add(new Element("sheet-content") { Children = cb.Build() });
        return this;
    }

    public ISheetBuilder Footer(Action<IContentBuilder> content)
    {
        var cb = new ContentBuilder();
        content(cb);
        sheet.Children.Add(new Element("sheet-footer") { Children = cb.Build() });
        return this;
    }
}

// ── Breadcrumb Builder ──────────────────────────────────────────────────

internal sealed class BreadcrumbBuilder(Element breadcrumb) : IBreadcrumbBuilder
{
    public IBreadcrumbBuilder Item(string label, string? href = null, string? icon = null)
    {
        breadcrumb.Children.Add(new Element("breadcrumb-item") { Text = label,
            Props = { ["href"] = href, ["icon"] = icon } });
        return this;
    }

    public IBreadcrumbBuilder Separator()
    {
        breadcrumb.Children.Add(new Element("breadcrumb-separator"));
        return this;
    }
}

// ── Menubar Builder ─────────────────────────────────────────────────────

internal sealed class MenubarBuilder : IMenubarBuilder
{
    internal readonly MenubarDescriptor Descriptor = new();

    public IMenubarBuilder Menu(string label, Action<IDropdownMenuBuilder> configure)
    {
        var menu = new Element("menubar-menu") { Text = label };
        var builder = new DropdownMenuBuilder(menu);
        configure(builder);
        Descriptor.Menus.Add(new MenubarMenuDescriptor { Label = label, Items = menu.Children });
        return this;
    }
}

// ── Navigation Menu Builder ─────────────────────────────────────────────

internal sealed class NavigationMenuBuilder : INavigationMenuBuilder
{
    internal readonly NavigationMenuDescriptor Descriptor = new();
    private readonly NavMenuGroupDescriptor? _currentGroup;

    internal NavigationMenuBuilder() { }

    private NavigationMenuBuilder(NavMenuGroupDescriptor group)
    {
        _currentGroup = group;
    }

    public INavigationMenuBuilder Item(string label, string href, string? description = null, string? icon = null)
    {
        var item = new NavMenuItemDescriptor
        {
            Label = label,
            Href = href,
            Description = description,
            Icon = icon
        };
        if (_currentGroup != null)
            _currentGroup.Items.Add(item);
        else
        {
            var defaultGroup = Descriptor.Groups.FirstOrDefault(g => g.Title == null);
            if (defaultGroup == null)
            {
                defaultGroup = new NavMenuGroupDescriptor();
                Descriptor.Groups.Add(defaultGroup);
            }
            defaultGroup.Items.Add(item);
        }
        return this;
    }

    public INavigationMenuBuilder Group(string title, Action<INavigationMenuBuilder> configure)
    {
        var group = new NavMenuGroupDescriptor { Title = title };
        var groupBuilder = new NavigationMenuBuilder(group);
        configure(groupBuilder);
        Descriptor.Groups.Add(group);
        return this;
    }
}

