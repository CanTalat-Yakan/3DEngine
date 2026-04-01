namespace Editor.Shell;

// ═══════════════════════════════════════════════════════════════════════════
//  CSS Builder — fluent Tailwind-style API for composing CSS classes.
//  Works with the existing string? css parameters via implicit conversion.
//
//  Usage:
//    css: Css.Flex().Column().Items(Align.Center).Gap(2)
//    css: Css.TextCenter().SpaceY(4)
//    css: Css.Container().PaddingY(10)
//
//  The raw string parameter remains as an advanced escape hatch:
//    css: "custom-class another-class"
// ═══════════════════════════════════════════════════════════════════════════

// ── Enums ───────────────────────────────────────────────────────────────

public enum Align { Start, Center, End, Stretch, Baseline }
public enum Justify { Start, Center, End, Between, Around, Evenly }

// ── Static entry point ──────────────────────────────────────────────────

/// <summary>
/// Static entry point for the fluent CSS builder. Each method creates a new
/// <see cref="CssBuilder"/> and adds the first class. Chain more classes with dot notation.
/// </summary>
public static class Css
{
    // Display
    public static CssBuilder Flex() => new CssBuilder().Flex();
    public static CssBuilder InlineFlex() => new CssBuilder().InlineFlex();
    public static CssBuilder Grid() => new CssBuilder().Grid();
    public static CssBuilder Block() => new CssBuilder().Block();
    public static CssBuilder InlineBlock() => new CssBuilder().InlineBlock();
    public static CssBuilder Hidden() => new CssBuilder().Hidden();
    public static CssBuilder Container() => new CssBuilder().Container();

    // Spacing starters
    public static CssBuilder SpaceX(int size) => new CssBuilder().SpaceX(size);
    public static CssBuilder SpaceY(int size) => new CssBuilder().SpaceY(size);
    public static CssBuilder Gap(int size) => new CssBuilder().Gap(size);

    // Margin starters
    public static CssBuilder MarginXAuto() => new CssBuilder().MarginXAuto();

    // Text starters
    public static CssBuilder TextCenter() => new CssBuilder().TextCenter();
    public static CssBuilder TextLeft() => new CssBuilder().TextLeft();
    public static CssBuilder TextRight() => new CssBuilder().TextRight();

    // Text size starters
    public static CssBuilder TextXs() => new CssBuilder().TextXs();
    public static CssBuilder TextSm() => new CssBuilder().TextSm();
    public static CssBuilder TextBase() => new CssBuilder().TextBase();
    public static CssBuilder TextLg() => new CssBuilder().TextLg();
    public static CssBuilder TextXl() => new CssBuilder().TextXl();

    // Position starters
    public static CssBuilder Relative() => new CssBuilder().Relative();
    public static CssBuilder Absolute() => new CssBuilder().Absolute();
    public static CssBuilder Fixed() => new CssBuilder().Fixed();
    public static CssBuilder Sticky() => new CssBuilder().Sticky();

    // Sizing starters
    public static CssBuilder WidthFull() => new CssBuilder().WidthFull();
    public static CssBuilder HeightFull() => new CssBuilder().HeightFull();

    // Padding starters
    public static CssBuilder Padding(int size) => new CssBuilder().Padding(size);
    public static CssBuilder PaddingX(int size) => new CssBuilder().PaddingX(size);
    public static CssBuilder PaddingY(int size) => new CssBuilder().PaddingY(size);
}

// ── Fluent builder ──────────────────────────────────────────────────────

/// <summary>
/// Fluent CSS class builder. Chain methods to compose Tailwind utility classes.
/// Implicitly converts to <c>string</c> so it works with any <c>string? css</c> parameter.
/// </summary>
public sealed class CssBuilder
{
    private readonly List<string> _classes = [];

    private CssBuilder Add(string cls) { _classes.Add(cls); return this; }

    // ── Display ─────────────────────────────────────────────────────

    public CssBuilder Flex() => Add("flex");
    public CssBuilder InlineFlex() => Add("inline-flex");
    public CssBuilder Grid() => Add("grid");
    public CssBuilder Block() => Add("block");
    public CssBuilder InlineBlock() => Add("inline-block");
    public CssBuilder Hidden() => Add("hidden");
    public CssBuilder Container() => Add("container");

    // ── Flex direction & wrap ───────────────────────────────────────

    public CssBuilder Column() => Add("flex-col");
    public CssBuilder Row() => Add("flex-row");
    public CssBuilder ColumnReverse() => Add("flex-col-reverse");
    public CssBuilder RowReverse() => Add("flex-row-reverse");
    public CssBuilder Wrap() => Add("flex-wrap");
    public CssBuilder NoWrap() => Add("flex-nowrap");
    public CssBuilder Flex1() => Add("flex-1");
    public CssBuilder Grow() => Add("grow");
    public CssBuilder Shrink() => Add("shrink");
    public CssBuilder Shrink0() => Add("shrink-0");

    // ── Alignment ───────────────────────────────────────────────────

    public CssBuilder Items(Align align) => Add($"items-{ToValue(align)}");
    public CssBuilder Justify(Shell.Justify justify) => Add($"justify-{ToValue(justify)}");
    public CssBuilder Self(Align align) => Add($"self-{ToValue(align)}");

    // ── Gap ─────────────────────────────────────────────────────────

    public CssBuilder Gap(int size) => Add($"gap-{size}");
    public CssBuilder GapX(int size) => Add($"gap-x-{size}");
    public CssBuilder GapY(int size) => Add($"gap-y-{size}");

    // ── Space between ───────────────────────────────────────────────

    public CssBuilder SpaceX(int size) => Add($"space-x-{size}");
    public CssBuilder SpaceY(int size) => Add($"space-y-{size}");

    // ── Padding ─────────────────────────────────────────────────────

    public CssBuilder Padding(int size) => Add($"p-{size}");
    public CssBuilder PaddingX(int size) => Add($"px-{size}");
    public CssBuilder PaddingY(int size) => Add($"py-{size}");
    public CssBuilder PaddingTop(int size) => Add($"pt-{size}");
    public CssBuilder PaddingBottom(int size) => Add($"pb-{size}");
    public CssBuilder PaddingLeft(int size) => Add($"pl-{size}");
    public CssBuilder PaddingRight(int size) => Add($"pr-{size}");

    // ── Margin ──────────────────────────────────────────────────────

    public CssBuilder Margin(int size) => Add($"m-{size}");
    public CssBuilder MarginX(int size) => Add($"mx-{size}");
    public CssBuilder MarginY(int size) => Add($"my-{size}");
    public CssBuilder MarginTop(int size) => Add($"mt-{size}");
    public CssBuilder MarginBottom(int size) => Add($"mb-{size}");
    public CssBuilder MarginLeft(int size) => Add($"ml-{size}");
    public CssBuilder MarginRight(int size) => Add($"mr-{size}");
    public CssBuilder MarginXAuto() => Add("mx-auto");
    public CssBuilder MarginYAuto() => Add("my-auto");
    public CssBuilder MarginAuto() => Add("m-auto");

    // ── Sizing ──────────────────────────────────────────────────────

    public CssBuilder Width(string value) => Add($"w-{value}");
    public CssBuilder Height(string value) => Add($"h-{value}");
    public CssBuilder WidthFull() => Add("w-full");
    public CssBuilder HeightFull() => Add("h-full");
    public CssBuilder MinWidth(string value) => Add($"min-w-{value}");
    public CssBuilder MaxWidth(string value) => Add($"max-w-{value}");
    public CssBuilder MinHeight(string value) => Add($"min-h-{value}");
    public CssBuilder MaxHeight(string value) => Add($"max-h-{value}");

    // ── Text alignment ──────────────────────────────────────────────

    public CssBuilder TextCenter() => Add("text-center");
    public CssBuilder TextLeft() => Add("text-left");
    public CssBuilder TextRight() => Add("text-right");
    public CssBuilder TextJustify() => Add("text-justify");

    // ── Text size ───────────────────────────────────────────────────

    public CssBuilder TextXs() => Add("text-xs");
    public CssBuilder TextSm() => Add("text-sm");
    public CssBuilder TextBase() => Add("text-base");
    public CssBuilder TextLg() => Add("text-lg");
    public CssBuilder TextXl() => Add("text-xl");
    public CssBuilder Text2xl() => Add("text-2xl");
    public CssBuilder Text3xl() => Add("text-3xl");
    public CssBuilder Text4xl() => Add("text-4xl");

    // ── Text color ──────────────────────────────────────────────────

    /// <summary>Adds <c>text-{color}</c>, e.g. <c>TextColor("muted-foreground")</c>.</summary>
    public CssBuilder TextColor(string color) => Add($"text-{color}");

    // ── Font weight ─────────────────────────────────────────────────

    public CssBuilder FontThin() => Add("font-thin");
    public CssBuilder FontLight() => Add("font-light");
    public CssBuilder FontNormal() => Add("font-normal");
    public CssBuilder FontMedium() => Add("font-medium");
    public CssBuilder FontSemibold() => Add("font-semibold");
    public CssBuilder FontBold() => Add("font-bold");
    public CssBuilder FontExtrabold() => Add("font-extrabold");

    // ── Border & rounding ───────────────────────────────────────────

    public CssBuilder Border() => Add("border");
    public CssBuilder BorderTop() => Add("border-t");
    public CssBuilder BorderBottom() => Add("border-b");
    public CssBuilder BorderLeft() => Add("border-l");
    public CssBuilder BorderRight() => Add("border-r");
    public CssBuilder Rounded() => Add("rounded");
    public CssBuilder RoundedSm() => Add("rounded-sm");
    public CssBuilder RoundedMd() => Add("rounded-md");
    public CssBuilder RoundedLg() => Add("rounded-lg");
    public CssBuilder RoundedXl() => Add("rounded-xl");
    public CssBuilder RoundedFull() => Add("rounded-full");

    // ── Position ────────────────────────────────────────────────────

    public CssBuilder Relative() => Add("relative");
    public CssBuilder Absolute() => Add("absolute");
    public CssBuilder Fixed() => Add("fixed");
    public CssBuilder Sticky() => Add("sticky");

    // ── Overflow ────────────────────────────────────────────────────

    public CssBuilder OverflowHidden() => Add("overflow-hidden");
    public CssBuilder OverflowAuto() => Add("overflow-auto");
    public CssBuilder OverflowScroll() => Add("overflow-scroll");

    // ── Truncate / whitespace ───────────────────────────────────────

    public CssBuilder Truncate() => Add("truncate");
    public CssBuilder WhitespaceNowrap() => Add("whitespace-nowrap");

    // ── Cursor ──────────────────────────────────────────────────────

    public CssBuilder CursorPointer() => Add("cursor-pointer");
    public CssBuilder CursorDefault() => Add("cursor-default");

    // ── Opacity ─────────────────────────────────────────────────────

    public CssBuilder Opacity(int percent) => Add($"opacity-{percent}");

    // ── Raw escape hatch ────────────────────────────────────────────

    /// <summary>Append arbitrary CSS classes for edge cases not covered by the builder.</summary>
    public CssBuilder Raw(string classes) => Add(classes);

    // ── Conversion ──────────────────────────────────────────────────

    public override string ToString() => string.Join(" ", _classes);
    public static implicit operator string(CssBuilder builder) => builder.ToString();

    // ── Helpers ─────────────────────────────────────────────────────

    private static string ToValue(Align a) => a switch
    {
        Align.Start => "start",
        Align.Center => "center",
        Align.End => "end",
        Align.Stretch => "stretch",
        Align.Baseline => "baseline",
        _ => "start"
    };

    private static string ToValue(Shell.Justify j) => j switch
    {
        Shell.Justify.Start => "start",
        Shell.Justify.Center => "center",
        Shell.Justify.End => "end",
        Shell.Justify.Between => "between",
        Shell.Justify.Around => "around",
        Shell.Justify.Evenly => "evenly",
        _ => "start"
    };
}

