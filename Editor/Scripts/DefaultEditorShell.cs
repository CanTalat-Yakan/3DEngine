using Editor.Shell;

[EditorShell]
public class DefaultEditorShell : IEditorShellBuilder
{
    public int Order => 0;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-home", "Home", DockZone.Center, panel =>
        {
            panel.Content(ui =>
            {
                ui.Div(Css.Container().PaddingY(10), container =>
                {
                    container.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(8), inner =>
                    {
                        // ── Hero Section ────────────────────────────────────
                        inner.Div(Css.TextCenter().SpaceY(4), hero =>
                        {
                            hero.Div(Css.Flex().Column().Items(Align.Center).Gap(2), badges =>
                            {
                                badges.Badge(Css.Default, "Editor Shell API", Variant.From(BadgeStyle.Secondary));
                                badges.Paragraph(Css.TextXs().TextColor("muted-foreground"),
                                    "Built with IContentBuilder · Zero Blazor dependency");
                            });
                            hero.Heading(Css.Default, 1, "Welcome to BlazorBlueprint");
                            hero.Paragraph(Css.Default, "Your Blazor app is ready with 80+ beautiful components.");
                        });

                        // ── Next Steps ──────────────────────────────────────
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Next Steps");
                            card.Description("Resources to help you get started.");
                            card.Content(c =>
                            {
                                c.Grid(Css.Default, 2, grid =>
                                {
                                    grid.Link(Css.Default, "Documentation", "https://blazorblueprintui.com/docs",
                                        Icon.From(Lucide.BookOpen), "Learn how to use all 80+ components.");
                                    grid.Link(Css.Default, "Components", "https://blazorblueprintui.com/components",
                                        Icon.From(Lucide.LayoutGrid), "Browse the complete component library.");
                                    grid.Link(Css.Default, "Showcase", "/showcase/buttons",
                                        Icon.From(Lucide.Eye), "See components in action with examples.");
                                    grid.Link(Css.Default, "GitHub", "https://github.com/blazorblueprintui/ui",
                                        Icon.From(Lucide.Github), "Star the repo and contribute.");
                                });
                            });
                        });
                    });
                });
            });
            panel.Icon(Icon.From(Lucide.House)).TabGroup("showcase", 0).Closeable(false);
        });
    }
}
