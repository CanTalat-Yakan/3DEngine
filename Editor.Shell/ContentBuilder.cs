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

    public IContentBuilder Icon(string? css, IconRef icon, int size = 16)
    { _elements.Add(new Element("icon") { Text = icon.Name, Css = css, Props = { ["size"] = size, ["iconRef"] = icon } }); return this; }

    public IContentBuilder Badge(string? css, string text, string? variant = null)
    { _elements.Add(new Element("badge") { Text = text, Css = css, Props = { ["variant"] = variant } }); return this; }

    public IContentBuilder Code(string? css, string text)
    { _elements.Add(new Element("code") { Text = text, Css = css }); return this; }

    public IContentBuilder Avatar(string? css, string? fallback = null, string? src = null)
    { _elements.Add(new Element("avatar") { Text = fallback, Css = css, Props = { ["src"] = src } }); return this; }

    public IContentBuilder Progress(string? css, int value, int max = 100)
    { _elements.Add(new Element("progress") { Css = css, Props = { ["value"] = value, ["max"] = max } }); return this; }

    // ── Interactive ─────────────────────────────────────────────────────

    public IContentBuilder Button(string? css, string label, Action? onClick = null, string? variant = null, IconRef icon = default,
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
        IconRef icon = default)
    {
        var el = new Element("alert") { Css = css, Props = { ["variant"] = variant, ["icon"] = icon } };
        if (title != null) el.Children.Add(new Element("alert-title") { Text = title });
        if (description != null) el.Children.Add(new Element("alert-description") { Text = description });
        _elements.Add(el);
        return this;
    }

    // ── Links ───────────────────────────────────────────────────────────

    public IContentBuilder Link(string? css, string text, string href, IconRef icon = default, string? description = null)
    {
        _elements.Add(new Element("link") { Text = text, Css = css,
            Props = { ["href"] = href, ["icon"] = icon, ["description"] = description } });
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

    public IContentBuilder TreeItem(string label, IconRef icon = default, bool selected = false, bool expanded = true,
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

    public IContentBuilder EmptyState(IconRef icon = default, string? title = null, string? description = null)
    {
        _elements.Add(new Element("empty-state") { Props = { ["icon"] = icon, ["title"] = title, ["description"] = description } });
        return this;
    }

    public IContentBuilder LogEntry(string time, string level, string category, string message)
    {
        _elements.Add(new Element("log-entry") { Props = { ["time"] = time, ["level"] = level, ["category"] = category, ["message"] = message } });
        return this;
    }

    // ── Helpers ──────────────────────────────────────────────────────────

    private static Element BuildContainer(string tag, string? css, Action<IContentBuilder> children)
    {
        var cb = new ContentBuilder();
        children(cb);
        return new Element(tag) { Css = css, Children = cb.Build() };
    }
}

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
    public ITabsBuilder Tab(string label, Action<IContentBuilder> content, IconRef icon = default)
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
