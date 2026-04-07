using Editor.Shell;

/// <summary>
/// Default editor shell that defines the "Home" landing page panel.
/// Demonstrates various <see cref="IContentBuilder"/> components including
/// buttons, inputs, toggle controls, badges, dialogs, and alert banners.
/// </summary>
/// <remarks>
/// This shell is loaded with <see cref="IEditorShellBuilder.Order"/> = 0 (default priority)
/// and registers a center-docked panel at the root route (<c>/</c>).
/// Serves as both a functional landing page and a living reference for the shell API.
/// </remarks>
/// <seealso cref="IEditorShellBuilder"/>
/// <seealso cref="ShowcasePageHelper"/>
[EditorShell]
public class DefaultEditorShell : IEditorShellBuilder
{
    /// <inheritdoc />
    public int Order => 0;

    /// <inheritdoc />
    public void Build(IShellBuilder shell)
    {
        // ── Home panel ───────────────────────────────────────────────
        shell.Panel("showcase-home", "Home", DockZone.Center, panel =>
        {
            panel.Icon(Icon.From(Lucide.House)).TabGroup("showcase", 0).Closeable(false).Route("/");
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

                        // ── Quick Demo (like App1) ───────────────────────────
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
                                    section.Label(Css.Default, "Input", forId: "demo-input");
                                    section.Input(Css.Default, placeholder: "Type something...", id: "demo-input");
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

                                // Dialog
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Dialog");
                                    section.Dialog("Open Dialog", dialog =>
                                    {
                                        dialog.Title("Edit Profile");
                                        dialog.Description("Make changes to your profile here. Click save when you're done.");
                                        dialog.Content(dc =>
                                        {
                                            dc.Div(Css.SpaceY(2), fields =>
                                            {
                                                fields.Label(Css.Default, "Name");
                                                fields.Input(Css.Default, value: "John Doe");
                                            });
                                            dc.Div(Css.SpaceY(2), fields =>
                                            {
                                                fields.Label(Css.Default, "Email");
                                                fields.Input(Css.Default, value: "john@example.com");
                                            });
                                        });
                                        dialog.Footer(df => df.Button(Css.Default, "Save changes", () => { }));
                                    }, triggerVariant: Variant.From(ButtonStyle.Outline));
                                });

                                c.Separator();

                                // Alert
                                c.Alert(Css.Default, "Everything is working!",
                                    "BlazorBlueprint components are ready to use. Start building something amazing.");
                            });
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
        });

    }
}
