using Editor.Shell;

[EditorShell]
public class ShowcaseButtonsShell : IEditorShellBuilder
{
    public int Order => 1;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-buttons", "Buttons", DockZone.Center, panel =>
        {
            panel.Content(ui =>
            {
                ui.Div(Css.Container().PaddingY(6), container =>
                {
                    container.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(6), inner =>
                    {
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Buttons");
                            card.Description("Displays a button or a component that looks like a button.");
                            card.Content(c =>
                            {
                                // Variants
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Variants");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Button(Css.Default, "Primary", () => { });
                                        row.Button(Css.Default, "Secondary", () => { }, Variant.From(ButtonStyle.Secondary));
                                        row.Button(Css.Default, "Outline", () => { }, Variant.From(ButtonStyle.Outline));
                                        row.Button(Css.Default, "Ghost", () => { }, Variant.From(ButtonStyle.Ghost));
                                        row.Button(Css.Default, "Link", () => { }, Variant.From(ButtonStyle.Link));
                                        row.Button(Css.Default, "Destructive", () => { }, Variant.From(ButtonStyle.Destructive));
                                    });
                                });

                                c.Separator();

                                // With Icons
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "With Icons");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Button(Css.Default, "Login with Email", () => { }, icon: Icon.From(Lucide.Mail));
                                        row.Button(Css.Default, "GitHub", () => { }, Variant.From(ButtonStyle.Outline), icon: Icon.From(Lucide.Github));
                                        row.Button(Css.Default, "Download", () => { }, Variant.From(ButtonStyle.Secondary), icon: Icon.From(Lucide.Download));
                                    });
                                });

                                c.Separator();

                                // States
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "States");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Button(Css.Default, "Disabled", disabled: true);
                                        row.Button(Css.Default, "Disabled Outline", disabled: true, variant: Variant.From(ButtonStyle.Outline));
                                        row.Button(Css.Default, "Loading...", loading: true);
                                    });
                                });

                                c.Separator();

                                // As Link
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "As Link");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Button(Css.Default, "Go Home", href: "/");
                                        row.Button(Css.Default, "Visit BlazorBlueprint", href: "https://blazorblueprintui.com",
                                            variant: Variant.From(ButtonStyle.Outline), icon: Icon.From(Lucide.ExternalLink));
                                    });
                                });
                            });
                        });
                    });
                });
            });
            panel.Icon(Icon.From(Lucide.RectangleHorizontal)).TabGroup("showcase", 1).Closeable(false);
        });
    }
}

