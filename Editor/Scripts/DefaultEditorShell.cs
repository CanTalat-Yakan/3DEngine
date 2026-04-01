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
                ui.Div(container =>
                {
                    container.Div(inner =>
                    {
                        // Hero Section
                        inner.Div(hero =>
                        {
                            hero.Div(badges =>
                            {
                                badges.Badge("Editor Shell API", variant: "secondary");
                                badges.Paragraph("Built with IContentBuilder · Zero Blazor dependency",
                                    Css.TextXs().TextColor("muted-foreground"));
                            }, Css.Flex().Column().Items(Align.Center).Gap(2));
                            hero.Heading(1, "Welcome to BlazorBlueprint");
                            hero.Paragraph("Your Blazor app is ready with 80+ beautiful components.");
                        }, Css.TextCenter().SpaceY(4));

                        // Quick Demo Card
                        inner.Card(card =>
                        {
                            card.Title("Quick Demo");
                            card.Description("Here are some BlazorBlueprint components in action.");
                            card.Content(c =>
                            {
                                // Buttons
                                c.Div(section =>
                                {
                                    section.Label("Buttons");
                                    section.Row(row =>
                                    {
                                        row.Button("Primary", () => { });
                                        row.Button("Secondary", variant: "secondary");
                                        row.Button("Outline", variant: "outline");
                                        row.Button("Ghost", variant: "ghost");
                                        row.Button("Destructive", variant: "destructive");
                                    });
                                }, Css.SpaceY(2));

                                c.Separator();

                                // Input
                                c.Div(section =>
                                {
                                    section.Label("Input");
                                    section.Input(placeholder: "Type something...");
                                }, Css.SpaceY(2));

                                c.Separator();

                                // Toggle Controls
                                c.Div(section =>
                                {
                                    section.Label("Toggle Controls");
                                    section.Checkbox("I agree to the terms and conditions", initial: false);
                                    section.Switch("Enable notifications", initial: true);
                                }, Css.SpaceY(4));

                                c.Separator();

                                // Badges
                                c.Div(section =>
                                {
                                    section.Label("Badges");
                                    section.Row(row =>
                                    {
                                        row.Badge("Default");
                                        row.Badge("Secondary", variant: "secondary");
                                        row.Badge("Outline", variant: "outline");
                                        row.Badge("Destructive", variant: "destructive");
                                    });
                                }, Css.SpaceY(2));

                                c.Separator();

                                // Alert
                                c.Alert("Everything is working!",
                                    "BlazorBlueprint components are ready to use. Start building something amazing.");
                            });
                        });

                        // Next Steps Card
                        inner.Card(card =>
                        {
                            card.Title("Next Steps");
                            card.Description("Resources to help you get started.");
                            card.Content(c =>
                            {
                                c.Grid(2, grid =>
                                {
                                    grid.Link("Documentation", "https://blazorblueprintui.com/docs",
                                        icon: "book-open", description: "Learn how to use all 80+ components.");
                                    grid.Link("Components", "https://blazorblueprintui.com/components",
                                        icon: "layout-grid", description: "Browse the complete component library.");
                                    grid.Link("Showcase", "/showcase/buttons",
                                        icon: "eye", description: "See components in action with examples.");
                                    grid.Link("GitHub", "https://github.com/blazorblueprintui/ui",
                                        icon: "github", description: "Star the repo and contribute.");
                                });
                            });
                        });

                    }, Css.MarginXAuto().MaxWidth("3xl").SpaceY(8));
                }, Css.Container().PaddingY(10));
            });
            panel.Icon("layout-grid").Closeable(false);
        });
    }
}
