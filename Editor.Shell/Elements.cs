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
    IContentBuilder Text(string? css, string text);
    IContentBuilder Heading(string? css, int level, string text);
    IContentBuilder Paragraph(string? css, string text);
    IContentBuilder Label(string? css, string text, string? forId = null);
    IContentBuilder Icon(string? css, string? icon, int size = 16);
    IContentBuilder Badge(string? css, string text, string? variant = null);
    IContentBuilder Code(string? css, string text);
    IContentBuilder Avatar(string? css, string? fallback = null, string? src = null);
    IContentBuilder Progress(string? css, int value, int max = 100);

    // ── Interactive ─────────────────────────────────────────────────────
    IContentBuilder Button(string? css, string label, Action? onClick = null, string? variant = null, string? icon = null,
        bool disabled = false, bool loading = false, string? href = null);
    IContentBuilder Input(string? css, string? placeholder = null, string? value = null, Action<string>? onChanged = null, string? id = null);
    IContentBuilder Textarea(string? css, string? placeholder = null, string? value = null, Action<string>? onChanged = null, string? id = null);
    IContentBuilder Checkbox(string? css, string? label = null, bool initial = false, Action<bool>? onChanged = null, string? id = null);
    IContentBuilder Switch(string? css, string? label = null, bool initial = false, Action<bool>? onChanged = null, string? id = null);
    IContentBuilder Select(string? css, (string Value, string Label)[] options, string? placeholder = null, string? selected = null,
        Action<string>? onChanged = null, string? id = null);
    IContentBuilder RadioGroup(string? css, (string Value, string Label)[] options, string? selected = null,
        Action<string>? onChanged = null);

    // ── Layout ──────────────────────────────────────────────────────────
    IContentBuilder Separator(string? css = null);
    IContentBuilder Spacer();
    IContentBuilder Div(string? css, Action<IContentBuilder> children);
    IContentBuilder Row(string? css, Action<IContentBuilder> children);
    IContentBuilder Column(string? css, Action<IContentBuilder> children);
    IContentBuilder Grid(string? css, int columns, Action<IContentBuilder> children);
    IContentBuilder ScrollArea(string? css, Action<IContentBuilder> children);

    // ── Cards ───────────────────────────────────────────────────────────
    IContentBuilder Card(string? css, Action<ICardBuilder> configure);

    // ── Feedback ────────────────────────────────────────────────────────
    IContentBuilder Alert(string? css, string? title = null, string? description = null, string? variant = null,
        string? icon = null);

    // ── Links ───────────────────────────────────────────────────────────
    IContentBuilder Link(string? css, string text, string href, string? icon = null, string? description = null);

    // ── Complex Components ──────────────────────────────────────────────
    IContentBuilder Tabs(string? css, Action<ITabsBuilder> configure);
    IContentBuilder Collapsible(string? css, string title, Action<IContentBuilder> content, bool expanded = false);
    IContentBuilder Dialog(string triggerLabel, Action<IDialogBuilder> configure, string? triggerVariant = null);
    IContentBuilder AlertDialog(string triggerLabel, Action<IAlertDialogBuilder> configure, string? triggerVariant = null);

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

/// <summary>Sub-builder for tab components.</summary>
public interface ITabsBuilder
{
    ITabsBuilder Tab(string label, Action<IContentBuilder> content, string? icon = null);
}

/// <summary>Sub-builder for dialog components.</summary>
public interface IDialogBuilder
{
    IDialogBuilder Title(string text);
    IDialogBuilder Description(string text);
    IDialogBuilder Content(Action<IContentBuilder> content);
    IDialogBuilder Footer(Action<IContentBuilder> content);
}

/// <summary>Sub-builder for alert dialog (confirmation) components.</summary>
public interface IAlertDialogBuilder
{
    IAlertDialogBuilder Title(string text);
    IAlertDialogBuilder Description(string text);
    IAlertDialogBuilder CancelText(string text);
    IAlertDialogBuilder ConfirmText(string text);
    IAlertDialogBuilder OnConfirm(Action action);
}
