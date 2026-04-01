using Editor.Shell;

[EditorShell]
public class ShowcaseNavigationShell : IEditorShellBuilder
{
    public int Order => 5;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-navigation", "Navigation", DockZone.Center, panel =>
        {
            panel.Content(ui =>
            {
                ui.Div(Css.Container().PaddingY(6), container =>
                {
                    container.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(6), inner =>
                    {
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Navigation");
                            card.Description("Tabs, collapsibles, accordion, breadcrumbs, and pagination.");
                            card.Content(c =>
                            {
                                // Tabs
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Tabs");
                                    section.Tabs(Css.Default, tabs =>
                                    {
                                        tabs.Tab("Account", content =>
                                        {
                                            content.Div(Css.SpaceY(2), inner2 =>
                                            {
                                                inner2.Label(Css.Default, "Name");
                                                inner2.Input(Css.Default, value: "John Doe");
                                                inner2.Label(Css.Default, "Email");
                                                inner2.Input(Css.Default, value: "john@example.com");
                                                inner2.Button(Css.Default, "Save changes", () => { });
                                            });
                                        }, icon: Icon.From(Lucide.User));
                                        tabs.Tab("Password", content =>
                                        {
                                            content.Div(Css.SpaceY(2), inner2 =>
                                            {
                                                inner2.Label(Css.Default, "Current password");
                                                inner2.Input(Css.Default, placeholder: "Enter current password");
                                                inner2.Label(Css.Default, "New password");
                                                inner2.Input(Css.Default, placeholder: "Enter new password");
                                                inner2.Button(Css.Default, "Change password", () => { });
                                            });
                                        }, icon: Icon.From(Lucide.Lock));
                                        tabs.Tab("Settings", content =>
                                        {
                                            content.Switch(Css.Default, "Dark mode", initial: false);
                                            content.Separator();
                                            content.Switch(Css.Default, "Notifications", initial: true);
                                        }, icon: Icon.From(Lucide.Settings));
                                    });
                                });

                                c.Separator();

                                // Accordion
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Accordion");
                                    section.Accordion(Css.Default, acc =>
                                    {
                                        acc.Item("what", "What is BlazorBlueprint?", content =>
                                        {
                                            content.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                                "BlazorBlueprint is a Blazor component library that provides 80+ pre-styled components following the shadcn/ui design system.");
                                        });
                                        acc.Item("install", "How do I install it?", content =>
                                        {
                                            content.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                                "You can install BlazorBlueprint via NuGet: dotnet add package BlazorBlueprint.Components");
                                        });
                                        acc.Item("customize", "Can I customize the styling?", content =>
                                        {
                                            content.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                                "Yes! BlazorBlueprint uses CSS variables for theming. You can customize colors, spacing, and other design tokens.");
                                        });
                                    });
                                });

                                c.Separator();

                                // Collapsible
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Collapsible");
                                    section.Collapsible(Css.Default, "Recent Repositories", content =>
                                    {
                                        content.Paragraph(Css.TextSm(), "blazorblueprint/components");
                                        content.Paragraph(Css.TextSm(), "blazorblueprint/primitives");
                                        content.Paragraph(Css.TextSm(), "blazorblueprint/icons");
                                    }, expanded: false);
                                });

                                c.Separator();

                                // Breadcrumb
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Breadcrumb");
                                    section.Breadcrumb(Css.Default, bc =>
                                    {
                                        bc.Item("Home", "/", Icon.From(Lucide.House));
                                        bc.Separator();
                                        bc.Item("Components", "/components");
                                        bc.Separator();
                                        bc.Item("Button");
                                    });
                                });

                                c.Separator();

                                // Pagination
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Pagination");
                                    section.Pagination(Css.Default, currentPage: 3, totalPages: 10);
                                });
                            });
                        });
                    });
                });
            });
            panel.Icon(Icon.From(Lucide.Navigation)).TabGroup("showcase", 5).Closeable(false);
        });
    }
}

