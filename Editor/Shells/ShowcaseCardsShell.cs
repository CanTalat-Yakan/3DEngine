using Editor.Shell;

[EditorShell]
public class ShowcaseCardsShell : IEditorShellBuilder
{
    public int Order => 3;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-cards", "Cards", DockZone.Center, panel =>
        {
            panel.Icon(Icon.From(Lucide.SquareStack)).TabGroup("showcase", 3).Closeable(false).Route("/showcase/cards");
            panel.Content(ui =>
            {
                ShowcasePageHelper.WrapWithSidebar(ui, content =>
                {
                    content.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(6), inner =>
                    {
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Cards");
                            card.Description("Cards with various layouts and interactive elements.");
                            card.Content(c =>
                            {
                                c.Grid(Css.Default, 2, grid =>
                                {
                                    grid.Card(Css.Default, feature =>
                                    {
                                        feature.Header(h => h.Icon(Css.Default, Icon.From(Lucide.Rocket), 24));
                                        feature.Title("Fast");
                                        feature.Description("Built with performance in mind.");
                                        feature.Content(fc => fc.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                            "Blazor's efficient rendering keeps your app snappy."));
                                    });

                                    grid.Card(Css.Default, feature =>
                                    {
                                        feature.Header(h => h.Icon(Css.Default, Icon.From(Lucide.ShieldCheck), 24));
                                        feature.Title("Secure");
                                        feature.Description("Type-safe components with full IntelliSense.");
                                        feature.Content(fc => fc.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                            "Catch errors at compile time, not runtime."));
                                    });

                                    grid.Card(Css.Default, feature =>
                                    {
                                        feature.Header(h => h.Icon(Css.Default, Icon.From(Lucide.Palette), 24));
                                        feature.Title("Beautiful");
                                        feature.Description("shadcn/ui inspired design.");
                                        feature.Content(fc => fc.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                            "Looks great out of the box, fully customizable."));
                                    });

                                    grid.Card(Css.Default, feature =>
                                    {
                                        feature.Header(h => h.Icon(Css.Default, Icon.From(Lucide.Puzzle), 24));
                                        feature.Title("Modular");
                                        feature.Description("Pick and choose what you need.");
                                        feature.Content(fc => fc.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                            "Each component works independently."));
                                    });
                                });

                                c.Separator();

                                // Interactive Card
                                c.Card(Css.Default, interactive =>
                                {
                                    interactive.Title("Notifications");
                                    interactive.Description("Manage your notification preferences.");
                                    interactive.Content(ic =>
                                    {
                                        ic.Switch(Css.Default, "Email notifications", initial: true);
                                        ic.Separator();
                                        ic.Switch(Css.Default, "Push notifications", initial: false);
                                        ic.Separator();
                                        ic.Switch(Css.Default, "SMS notifications", initial: false);
                                    });
                                    interactive.Footer(f => f.Button(Css.Default, "Save preferences", () => { }));
                                });
                            });
                        });
                    });
                });
            });
        });
    }
}

