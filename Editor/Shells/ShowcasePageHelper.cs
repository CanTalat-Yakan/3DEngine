using System;
using Editor.Shell;

/// <summary>
/// Helper used by showcase shell scripts to wrap page content with the
/// shared sidebar navigation layout.  Lives at the implementation (script)
/// level -- Editor.Server knows nothing about showcases.
/// </summary>
public static class ShowcasePageHelper
{
    private static readonly (string Label, string Href, string? Icon)[] Links =
    [
        ("Buttons",           "/showcase/buttons",    Icon.From(Lucide.RectangleHorizontal)),
        ("Forms",             "/showcase/forms",      Icon.From(Lucide.TextCursorInput)),
        ("Cards",             "/showcase/cards",      Icon.From(Lucide.SquareStack)),
        ("Alerts & Feedback", "/showcase/alerts",     Icon.From(Lucide.Bell)),
        ("Navigation",        "/showcase/navigation", Icon.From(Lucide.Navigation)),
        ("Dialogs & Overlays","/showcase/dialogs",    Icon.From(Lucide.PanelTop)),
    ];

    /// <summary>
    /// Wraps <paramref name="pageContent"/> inside a two-column layout:
    /// fixed sidebar (nav links) + scrollable main content area.
    /// </summary>
    public static void WrapWithSidebar(IContentBuilder ui, Action<IContentBuilder> pageContent)
    {
        ui.Div("container py-6", container =>
        {
            container.Div("flex flex-col lg:flex-row gap-8", layout =>
            {
                // ── Sidebar ────────────────────────────────────────
                layout.Div("lg:w-64 shrink-0", sidebar =>
                {
                    sidebar.Div("sticky top-20 space-y-1", nav =>
                    {
                        nav.Heading("font-semibold mb-3", 4, "Showcase");

                        foreach (var (label, href, icon) in Links)
                        {
                            nav.Button(
                                "flex items-center gap-2 rounded-md px-3 py-2 text-sm font-normal justify-start w-full hover:bg-accent",
                                label,
                                href: href,
                                variant: Variant.From(ButtonStyle.Ghost),
                                icon: icon);
                        }
                    });
                });

                // ── Content ────────────────────────────────────────
                layout.Div("flex-1 min-w-0", pageContent);
            });
        });
    }
}

