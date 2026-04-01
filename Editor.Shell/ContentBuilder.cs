namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Concrete Content Builder — builds Element trees from fluent API calls.
// ═══════════════════════════════════════════════════════════════════════════

public sealed class ContentBuilder : IContentBuilder
{
    private readonly List<Element> _elements = [];
    public List<Element> Build() => _elements;

    // ── Text & Display ──────────────────────────────────────────────────

    public IContentBuilder Text(string text, string? css = null)
    { _elements.Add(new Element("text") { Text = text, Css = css }); return this; }

    public IContentBuilder Heading(int level, string text, string? css = null)
    { _elements.Add(new Element($"h{Math.Clamp(level, 1, 6)}") { Text = text, Css = css }); return this; }

    public IContentBuilder Paragraph(string text, string? css = null)
    { _elements.Add(new Element("p") { Text = text, Css = css }); return this; }

    public IContentBuilder Label(string text, string? forId = null, string? css = null)
    { _elements.Add(new Element("label") { Text = text, Id = forId, Css = css }); return this; }

    public IContentBuilder Icon(string name, int size = 16, string? css = null)
    { _elements.Add(new Element("icon") { Text = name, Css = css, Props = { ["size"] = size } }); return this; }

    public IContentBuilder Badge(string text, string? variant = null, string? css = null)
    { _elements.Add(new Element("badge") { Text = text, Css = css, Props = { ["variant"] = variant } }); return this; }

    public IContentBuilder Code(string text, string? css = null)
    { _elements.Add(new Element("code") { Text = text, Css = css }); return this; }

    // ── Interactive ─────────────────────────────────────────────────────

    public IContentBuilder Button(string label, Action? onClick = null, string? variant = null, string? icon = null, string? css = null)
    {
        _elements.Add(new Element("button") { Text = label, OnClick = onClick, Css = css,
            Props = { ["variant"] = variant, ["icon"] = icon } });
        return this;
    }

    public IContentBuilder Input(string? placeholder = null, string? value = null, Action<string>? onChanged = null, string? id = null, string? css = null)
    {
        _elements.Add(new Element("input") { Id = id, Css = css, OnInput = onChanged,
            Props = { ["placeholder"] = placeholder, ["value"] = value } });
        return this;
    }

    public IContentBuilder Checkbox(string? label = null, bool initial = false, Action<bool>? onChanged = null, string? id = null, string? css = null)
    {
        _elements.Add(new Element("checkbox") { Text = label, Id = id, Css = css, OnToggle = onChanged,
            Props = { ["checked"] = initial } });
        return this;
    }

    public IContentBuilder Switch(string? label = null, bool initial = false, Action<bool>? onChanged = null, string? id = null, string? css = null)
    {
        _elements.Add(new Element("switch") { Text = label, Id = id, Css = css, OnToggle = onChanged,
            Props = { ["checked"] = initial } });
        return this;
    }

    // ── Layout ──────────────────────────────────────────────────────────

    public IContentBuilder Separator(string? css = null)
    { _elements.Add(new Element("separator") { Css = css }); return this; }

    public IContentBuilder Spacer()
    { _elements.Add(new Element("spacer")); return this; }

    public IContentBuilder Div(Action<IContentBuilder> children, string? css = null)
    { _elements.Add(BuildContainer("div", css, children)); return this; }

    public IContentBuilder Row(Action<IContentBuilder> children, string? css = null)
    { _elements.Add(BuildContainer("row", css, children)); return this; }

    public IContentBuilder Column(Action<IContentBuilder> children, string? css = null)
    { _elements.Add(BuildContainer("column", css, children)); return this; }

    public IContentBuilder Grid(int columns, Action<IContentBuilder> children, string? css = null)
    {
        var el = BuildContainer("grid", css, children);
        el.Props["columns"] = columns;
        _elements.Add(el);
        return this;
    }

    public IContentBuilder ScrollArea(Action<IContentBuilder> children, string? css = null)
    { _elements.Add(BuildContainer("scroll", css, children)); return this; }

    // ── Cards ───────────────────────────────────────────────────────────

    public IContentBuilder Card(Action<ICardBuilder> configure, string? css = null)
    {
        var card = new Element("card") { Css = css };
        var builder = new CardBuilder(card);
        configure(builder);
        _elements.Add(card);
        return this;
    }

    // ── Feedback ────────────────────────────────────────────────────────

    public IContentBuilder Alert(string? title = null, string? description = null, string? variant = null, string? css = null)
    {
        var el = new Element("alert") { Css = css, Props = { ["variant"] = variant } };
        if (title != null) el.Children.Add(new Element("alert-title") { Text = title });
        if (description != null) el.Children.Add(new Element("alert-description") { Text = description });
        _elements.Add(el);
        return this;
    }

    // ── Links ───────────────────────────────────────────────────────────

    public IContentBuilder Link(string text, string href, string? icon = null, string? description = null, string? css = null)
    {
        _elements.Add(new Element("link") { Text = text, Css = css,
            Props = { ["href"] = href, ["icon"] = icon, ["description"] = description } });
        return this;
    }

    // ── Editor-specific ─────────────────────────────────────────────────

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
