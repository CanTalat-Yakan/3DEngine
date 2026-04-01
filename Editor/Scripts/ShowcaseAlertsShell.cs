using Editor.Shell;

[EditorShell]
public class ShowcaseAlertsShell : IEditorShellBuilder
{
    public int Order => 4;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-alerts", "Alerts & Feedback", DockZone.Center, panel =>
        {
            panel.Content(ui =>
            {
                ui.Div(Css.Container().PaddingY(6), container =>
                {
                    container.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(6), inner =>
                    {
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Alerts & Feedback");
                            card.Description("Alert banners and toast notifications for various contexts.");
                            card.Content(c =>
                            {
                                c.Alert(Css.Default, "Default Alert",
                                    "This is a default alert. Use it for general information.",
                                    icon: Icon.From(Lucide.Terminal));

                                c.Alert(Css.Default, "Error",
                                    "Your session has expired. Please log in again.",
                                    variant: Variant.From(AlertStyle.Danger), icon: Icon.From(Lucide.CircleAlert));

                                c.Alert(Css.Default, "Information",
                                    "A new version is available. Refresh to update.",
                                    variant: Variant.From(AlertStyle.Info), icon: Icon.From(Lucide.Info));

                                c.Alert(Css.Default, "Success",
                                    "Your changes have been saved successfully.",
                                    variant: Variant.From(AlertStyle.Success), icon: Icon.From(Lucide.CircleCheck));

                                c.Alert(Css.Default, "Warning",
                                    "Your subscription will expire in 3 days.",
                                    variant: Variant.From(AlertStyle.Warning), icon: Icon.From(Lucide.TriangleAlert));

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

                                // Progress
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Progress");
                                    section.Progress(Css.Default, 25);
                                    section.Progress(Css.Default, 66);
                                    section.Progress(Css.Default, 100);
                                });

                                c.Separator();

                                // Code & Kbd
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Code & Keyboard Shortcuts");
                                    section.Code(Css.Default, "dotnet add package BlazorBlueprint.Components");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Kbd(Css.Default, "Ctrl+S");
                                        row.Kbd(Css.Default, "Ctrl+Z");
                                        row.Kbd(Css.Default, "Ctrl+Shift+P");
                                    });
                                });
                            });
                        });
                    });
                });
            });
            panel.Icon(Icon.From(Lucide.Bell)).TabGroup("showcase", 4).Closeable(false);
        });
    }
}

