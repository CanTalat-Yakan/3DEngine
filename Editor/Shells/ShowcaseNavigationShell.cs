using Editor.Shell;

[EditorShell]
public class ShowcaseNavigationShell : IEditorShellBuilder
{
    public int Order => 5;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-navigation", "Navigation", DockZone.Center, panel =>
        {
            panel.Icon(Icon.From(Lucide.Navigation)).TabGroup("showcase", 5).Closeable(false).Route("/showcase/navigation");
            panel.Content(ui =>
            {
                ShowcasePageHelper.WrapWithSidebar(ui, content =>
                {
                    content.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(6), inner =>
                    {
                        // ── Menubar ────────────────────────────────────────────
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Menubar");
                            card.Description("A horizontal menu bar with dropdown menus, typically at the top of a window or application.");
                            card.Content(c =>
                            {
                                c.Menubar(Css.Default, menubar =>
                                {
                                    menubar.Menu("File", menu =>
                                    {
                                        menu.Item("New Tab", shortcut: "⌘T");
                                        menu.Item("New Window", shortcut: "⌘N");
                                        menu.Separator();
                                        menu.Item("Share");
                                        menu.Separator();
                                        menu.Item("Print...", shortcut: "⌘P");
                                    });
                                    menubar.Menu("Edit", menu =>
                                    {
                                        menu.Item("Undo", shortcut: "⌘Z");
                                        menu.Item("Redo", shortcut: "⇧⌘Z");
                                        menu.Separator();
                                        menu.Item("Cut");
                                        menu.Item("Copy");
                                        menu.Item("Paste");
                                    });
                                    menubar.Menu("View", menu =>
                                    {
                                        menu.CheckboxItem("Always Show Bookmarks Bar", initial: true);
                                        menu.CheckboxItem("Always Show Full URLs", initial: false);
                                        menu.Separator();
                                        menu.Item("Reload", shortcut: "⌘R");
                                        menu.Item("Force Reload", shortcut: "⇧⌘R");
                                        menu.Separator();
                                        menu.Item("Toggle Fullscreen");
                                    });
                                    menubar.Menu("Profiles", menu =>
                                    {
                                        menu.Label("Switch Profile");
                                        menu.Separator();
                                        menu.Item("Andy");
                                        menu.Item("Benoit");
                                        menu.Item("Luis");
                                        menu.Separator();
                                        menu.Item("Edit Profiles...");
                                        menu.Item("Add Profile...");
                                    });
                                });
                            });
                        });

                        // ── Navigation Menu ────────────────────────────────────
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Navigation Menu");
                            card.Description("A collection of links for navigating websites, with support for grouped dropdown content.");
                            card.Content(c =>
                            {
                                c.NavigationMenu(Css.Default, nav =>
                                {
                                    nav.Group("Getting Started", group =>
                                    {
                                        group.Item("Introduction", "/docs",
                                            description: "Re-usable components built using Blazor and Tailwind CSS.",
                                            icon: Icon.From(Lucide.BookOpen));
                                        group.Item("Installation", "/docs/installation",
                                            description: "How to install dependencies and structure your app.",
                                            icon: Icon.From(Lucide.Download));
                                        group.Item("Typography", "/docs/typography",
                                            description: "Styles for headings, paragraphs, lists...etc.",
                                            icon: Icon.From(Lucide.Type));
                                    });
                                    nav.Group("Components", group =>
                                    {
                                        group.Item("Alert Dialog", "/docs/alert-dialog",
                                            description: "A modal dialog that interrupts the user with important content.");
                                        group.Item("Hover Card", "/docs/hover-card",
                                            description: "For sighted users to preview content available behind a link.");
                                        group.Item("Progress", "/docs/progress",
                                            description: "Displays an indicator showing the completion progress of a task.");
                                        group.Item("Tooltip", "/docs/tooltip",
                                            description: "A popup that displays information related to an element when focused or hovered.");
                                    });
                                    nav.Item("Documentation", "https://blazorblueprintui.com/docs");
                                });
                            });
                        });

                        // ── Tabs ────────────────────────────────────────────
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Tabs");
                            card.Description("Switch between different views or sections.");
                            card.Content(c =>
                            {
                                c.Tabs(Css.Default, tabs =>
                                {
                                    tabs.Tab("Account", content =>
                                    {
                                        content.Card(Css.Default, tabCard =>
                                        {
                                            tabCard.Title("Account");
                                            tabCard.Description("Make changes to your account here. Click save when you're done.");
                                            tabCard.Content(tc =>
                                            {
                                                tc.Div(Css.SpaceY(2), fields =>
                                                {
                                                    fields.Label(Css.Default, "Name");
                                                    fields.Input(Css.Default, value: "John Doe");
                                                });
                                                tc.Div(Css.SpaceY(2), fields =>
                                                {
                                                    fields.Label(Css.Default, "Email");
                                                    fields.Input(Css.Default, value: "john@example.com");
                                                });
                                            });
                                            tabCard.Footer(f => f.Button(Css.Default, "Save changes", () => { }));
                                        });
                                    });
                                    tabs.Tab("Password", content =>
                                    {
                                        content.Card(Css.Default, tabCard =>
                                        {
                                            tabCard.Title("Password");
                                            tabCard.Description("Change your password here. After saving, you'll be logged out.");
                                            tabCard.Content(tc =>
                                            {
                                                tc.Div(Css.SpaceY(2), fields =>
                                                {
                                                    fields.Label(Css.Default, "Current password");
                                                    fields.Input(Css.Default, placeholder: "Enter current password");
                                                });
                                                tc.Div(Css.SpaceY(2), fields =>
                                                {
                                                    fields.Label(Css.Default, "New password");
                                                    fields.Input(Css.Default, placeholder: "Enter new password");
                                                });
                                            });
                                            tabCard.Footer(f => f.Button(Css.Default, "Change password", () => { }));
                                        });
                                    });
                                    tabs.Tab("Settings", content =>
                                    {
                                        content.Card(Css.Default, tabCard =>
                                        {
                                            tabCard.Title("Settings");
                                            tabCard.Description("Manage your application settings and preferences.");
                                            tabCard.Content(tc =>
                                            {
                                                tc.Switch(Css.Default, "Dark mode", initial: false);
                                                tc.Separator();
                                                tc.Switch(Css.Default, "Notifications", initial: true);
                                            });
                                        });
                                    });
                                });
                            });
                        });

                        // ── Collapsible ─────────────────────────────────────
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Collapsible");
                            card.Description("A section that can be expanded or collapsed.");
                            card.Content(c =>
                            {
                                c.Collapsible(Css.Default, "Recent Repositories", content =>
                                {
                                    content.Paragraph(Css.TextSm(), "blazorblueprint/components");
                                    content.Paragraph(Css.TextSm(), "blazorblueprint/primitives");
                                    content.Paragraph(Css.TextSm(), "blazorblueprint/icons");
                                }, expanded: false);
                            });
                        });

                        // ── FAQ (Multiple Collapsibles) ─────────────────────
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("FAQ Section");
                            card.Description("Multiple collapsible sections for FAQ-style content.");
                            card.Content(c =>
                            {
                                c.Collapsible(Css.Default, "What is BlazorBlueprint?", content =>
                                {
                                    content.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                        "BlazorBlueprint is a Blazor component library that provides 80+ pre-styled components following the shadcn/ui design system. It includes both styled components and headless primitives.");
                                });
                                c.Collapsible(Css.Default, "How do I install it?", content =>
                                {
                                    content.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                        "You can install BlazorBlueprint via NuGet: dotnet add package BlazorBlueprint.Components");
                                });
                                c.Collapsible(Css.Default, "Can I customize the styling?", content =>
                                {
                                    content.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                        "Yes! BlazorBlueprint uses CSS variables for theming. You can customize colors, spacing, and other design tokens by modifying the CSS variables in your theme file.");
                                });
                            });
                        });

                        // ── Breadcrumb ──────────────────────────────────────
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Breadcrumb");
                            card.Description("Displays the path to the current page using a hierarchy of links.");
                            card.Content(c =>
                            {
                                c.Breadcrumb(Css.Default, bc =>
                                {
                                    bc.Item("Home", "/", Icon.From(Lucide.House));
                                    bc.Separator();
                                    bc.Item("Components", "/components");
                                    bc.Separator();
                                    bc.Item("Button");
                                });
                            });
                        });
                    });
                });
            });
        });
    }
}
