using Editor.Shell;

[EditorShell]
public class DefaultEditorShell : IEditorShellBuilder
{
    public int Order => 0;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase", "Home", DockZone.Center, panel =>
        {
            panel.Content(ui =>
            {
                ui.Div(Css.Container().PaddingY(10), container =>
                {
                    container.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(8), inner =>
                    {
                        // Hero Section
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

                        // Quick Demo Card
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Quick Demo");
                            card.Description("Here are some BlazorBlueprint components in action.");
                            card.Content(c =>
                            {
                                // Buttons
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Buttons");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Button(Css.Default, "Primary", () => { });
                                        row.Button(Css.Default, "Secondary", () => { }, Variant.From(ButtonStyle.Secondary));
                                        row.Button(Css.Default, "Outline", () => { }, Variant.From(ButtonStyle.Outline));
                                        row.Button(Css.Default, "Ghost", () => { }, Variant.From(ButtonStyle.Ghost));
                                        row.Button(Css.Default, "Destructive", () => { }, Variant.From(ButtonStyle.Destructive));
                                    });
                                });

                                c.Separator();

                                // Input
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Input");
                                    section.Input(Css.Default, placeholder: "Type something...");
                                });

                                c.Separator();

                                // Toggle Controls
                                c.Div(Css.SpaceY(4), section =>
                                {
                                    section.Label(Css.Default, "Toggle Controls");
                                    section.Checkbox(Css.Default, "I agree to the terms and conditions", initial: false);
                                    section.Switch(Css.Default, "Enable notifications", initial: true);
                                });

                                c.Separator();

                                // Badges
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Badges");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Badge(Css.Default, "Default");
                                        row.Badge(Css.Default, "Secondary", Variant.From(BadgeStyle.Secondary));
                                        row.Badge(Css.Default, "Outline", Variant.From(BadgeStyle.Outline));
                                        row.Badge(Css.Default, "Destructive", Variant.From(BadgeStyle.Destructive));
                                    });
                                });

                                c.Separator();

                                // Alert
                                c.Alert(Css.Default, "Everything is working!",
                                    "BlazorBlueprint components are ready to use. Start building something amazing.");
                            });
                        });

                        // Next Steps Card
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
            panel.Icon(Icon.From(Lucide.LayoutGrid)).Closeable(false);
        });
    }
}
