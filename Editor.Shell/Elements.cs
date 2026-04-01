namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  Element Descriptors — a virtual UI tree built from C# scripts.
//  Each node maps to a BlazorBlueprint component rendered by ElementRenderer.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>
/// A single UI element in the content tree. Tag determines the component type,
/// Props carry configuration, Children form the tree, and event handlers are first-class.
/// </summary>
public sealed class Element
{
    public string Tag { get; set; } = "div";
    public string? Css { get; set; }
    public string? Text { get; set; }
    public string? Id { get; set; }
    public Dictionary<string, object?> Props { get; set; } = [];
    public List<Element> Children { get; set; } = [];

    // Events
    public Action? OnClick { get; set; }
    public Action<bool>? OnToggle { get; set; }
    public Action<string>? OnInput { get; set; }

    public Element() { }
    public Element(string tag) => Tag = tag;
    public Element(string tag, string? text) { Tag = tag; Text = text; }
}

// ═══════════════════════════════════════════════════════════════════════════
//  Content Builder — fluent API for building UI element trees from C# scripts.
//  Covers all BlazorBlueprint component types: layout, text, inputs, cards,
//  alerts, badges, dialogs, trees, field rows, etc.
// ═══════════════════════════════════════════════════════════════════════════

/// <summary>Fluent builder for composing UI element trees. No Blazor dependency.</summary>
public interface IContentBuilder
{
    // ── Text & Display ──────────────────────────────────────────────────
    IContentBuilder Text(string text, string? css = null);
    IContentBuilder Heading(int level, string text, string? css = null);
    IContentBuilder Paragraph(string text, string? css = null);
    IContentBuilder Label(string text, string? forId = null, string? css = null);
    IContentBuilder Icon(string name, int size = 16, string? css = null);
    IContentBuilder Badge(string text, string? variant = null, string? css = null);
    IContentBuilder Code(string text, string? css = null);

    // ── Interactive ─────────────────────────────────────────────────────
    IContentBuilder Button(string label, Action? onClick = null, string? variant = null, string? icon = null, string? css = null);
    IContentBuilder Input(string? placeholder = null, string? value = null, Action<string>? onChanged = null, string? id = null, string? css = null);
    IContentBuilder Checkbox(string? label = null, bool initial = false, Action<bool>? onChanged = null, string? id = null, string? css = null);
    IContentBuilder Switch(string? label = null, bool initial = false, Action<bool>? onChanged = null, string? id = null, string? css = null);

    // ── Layout ──────────────────────────────────────────────────────────
    IContentBuilder Separator(string? css = null);
    IContentBuilder Spacer();
    IContentBuilder Div(Action<IContentBuilder> children, string? css = null);
    IContentBuilder Row(Action<IContentBuilder> children, string? css = null);
    IContentBuilder Column(Action<IContentBuilder> children, string? css = null);
    IContentBuilder Grid(int columns, Action<IContentBuilder> children, string? css = null);
    IContentBuilder ScrollArea(Action<IContentBuilder> children, string? css = null);

    // ── Cards ───────────────────────────────────────────────────────────
    IContentBuilder Card(Action<ICardBuilder> configure, string? css = null);

    // ── Feedback ────────────────────────────────────────────────────────
    IContentBuilder Alert(string? title = null, string? description = null, string? variant = null, string? css = null);

    // ── Links ───────────────────────────────────────────────────────────
    IContentBuilder Link(string text, string href, string? icon = null, string? description = null, string? css = null);

    // ── Editor-specific ─────────────────────────────────────────────────
    IContentBuilder TreeItem(string label, string? icon = null, bool selected = false, bool expanded = true,
        Action? onClick = null, Action<IContentBuilder>? children = null, string? iconColor = null);
    IContentBuilder FieldRow(string label, Action<IContentBuilder> control);
    IContentBuilder EmptyState(string? icon = null, string? title = null, string? description = null);
    IContentBuilder LogEntry(string time, string level, string category, string message);
}

/// <summary>Sub-builder for card structure (header, title, description, content, footer).</summary>
public interface ICardBuilder
{
    ICardBuilder Title(string text);
    ICardBuilder Description(string text);
    ICardBuilder Header(Action<IContentBuilder> content);
    ICardBuilder Content(Action<IContentBuilder> content);
    ICardBuilder Footer(Action<IContentBuilder> content);
    ICardBuilder Css(string css);
}
