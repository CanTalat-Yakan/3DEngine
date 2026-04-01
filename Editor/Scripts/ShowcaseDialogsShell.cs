using Editor.Shell;

[EditorShell]
public class ShowcaseDialogsShell : IEditorShellBuilder
{
    public int Order => 6;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-dialogs", "Dialogs & Overlays", DockZone.Center, panel =>
        {
            panel.Content(ui =>
            {
                ui.Div(Css.Container().PaddingY(6), container =>
                {
                    container.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(6), inner =>
                    {
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Dialogs & Overlays");
                            card.Description("Modal dialogs, drawers, sheets, tooltips, and menus.");
                            card.Content(c =>
                            {
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
                                            dc.Label(Css.Default, "Name");
                                            dc.Input(Css.Default, value: "John Doe");
                                            dc.Label(Css.Default, "Email");
                                            dc.Input(Css.Default, value: "john@example.com");
                                        });
                                        dialog.Footer(df => df.Button(Css.Default, "Save changes", () => { }));
                                    });
                                });

                                c.Separator();

                                // Alert Dialog
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Alert Dialog");
                                    section.AlertDialog("Delete Account", dialog =>
                                    {
                                        dialog.Title("Are you absolutely sure?");
                                        dialog.Description("This action cannot be undone. This will permanently delete your account and remove your data from our servers.");
                                        dialog.CancelText("Cancel");
                                        dialog.ConfirmText("Yes, delete account");
                                    }, triggerVariant: Variant.From(ButtonStyle.Destructive));
                                });

                                c.Separator();

                                // Drawer
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Drawer");
                                    section.Drawer("Open Drawer", drawer =>
                                    {
                                        drawer.Title("Settings");
                                        drawer.Description("Adjust your application preferences.");
                                        drawer.Content(dc =>
                                        {
                                            dc.Switch(Css.Default, "Dark mode", initial: false);
                                            dc.Separator();
                                            dc.Switch(Css.Default, "Notifications", initial: true);
                                            dc.Separator();
                                            dc.Slider(Css.Default, value: 75, min: 0, max: 100);
                                        });
                                    }, triggerVariant: Variant.From(ButtonStyle.Outline));
                                });

                                c.Separator();

                                // Sheet
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Sheet");
                                    section.Sheet("Open Sheet", sheet =>
                                    {
                                        sheet.Title("Navigation");
                                        sheet.Description("Browse pages and sections.");
                                        sheet.Content(sc =>
                                        {
                                            sc.Link(Css.Default, "Dashboard", "/dashboard", Icon.From(Lucide.LayoutDashboard));
                                            sc.Link(Css.Default, "Settings", "/settings", Icon.From(Lucide.Settings));
                                            sc.Link(Css.Default, "Profile", "/profile", Icon.From(Lucide.User));
                                        });
                                    }, triggerVariant: Variant.From(ButtonStyle.Outline));
                                });

                                c.Separator();

                                // Tooltip
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Tooltip");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Tooltip(Css.Default, trigger =>
                                        {
                                            trigger.Button(Css.Default, "Hover me", variant: Variant.From(ButtonStyle.Outline));
                                        }, "This is a tooltip!", side: Variant.From(Side.Top));
                                        row.Tooltip(Css.Default, trigger =>
                                        {
                                            trigger.Button(Css.Default, "Bottom tip", variant: Variant.From(ButtonStyle.Outline));
                                        }, "Tooltip on bottom", side: Variant.From(Side.Bottom));
                                    });
                                });

                                c.Separator();

                                // Dropdown Menu
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Dropdown Menu");
                                    section.DropdownMenu(Css.Default, "Open Menu", menu =>
                                    {
                                        menu.Label("My Account");
                                        menu.Separator();
                                        menu.Item("Profile", icon: Icon.From(Lucide.User), shortcut: "⇧⌘P");
                                        menu.Item("Settings", icon: Icon.From(Lucide.Settings), shortcut: "⌘S");
                                        menu.Item("Keyboard shortcuts", icon: Icon.From(Lucide.Keyboard), shortcut: "⌘K");
                                        menu.Separator();
                                        menu.Sub("More", submenu =>
                                        {
                                            submenu.Item("About");
                                            submenu.Item("Help Center");
                                            submenu.Item("Feedback");
                                        });
                                        menu.Separator();
                                        menu.Item("Log out", icon: Icon.From(Lucide.LogOut));
                                    }, triggerVariant: Variant.From(ButtonStyle.Outline));
                                });

                                c.Separator();

                                // Popover
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Popover");
                                    section.Popover(Css.Default,
                                        trigger => trigger.Button(Css.Default, "Open Popover", variant: Variant.From(ButtonStyle.Outline)),
                                        content =>
                                        {
                                            content.Heading(Css.Default, 4, "Dimensions");
                                            content.Paragraph(Css.TextSm().TextColor("muted-foreground"), "Set the dimensions for the layer.");
                                            content.Separator();
                                            content.FieldRow("Width", ctrl => ctrl.Input(Css.Default, value: "100%"));
                                            content.FieldRow("Height", ctrl => ctrl.Input(Css.Default, value: "25px"));
                                        });
                                });

                                c.Separator();

                                // HoverCard
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Hover Card");
                                    section.HoverCard(Css.Default,
                                        trigger => trigger.Button(Css.Default, "@blazorblueprint", variant: Variant.From(ButtonStyle.Link)),
                                        content =>
                                        {
                                            content.Heading(Css.Default, 4, "BlazorBlueprint");
                                            content.Paragraph(Css.TextSm().TextColor("muted-foreground"),
                                                "A beautiful Blazor component library with 80+ components. Built on shadcn/ui design principles.");
                                            content.Row(Css.Default, row =>
                                            {
                                                row.Badge(Css.Default, "Open Source", Variant.From(BadgeStyle.Secondary));
                                                row.Badge(Css.Default, ".NET", Variant.From(BadgeStyle.Outline));
                                            });
                                        });
                                });
                            });
                        });
                    });
                });
            });
            panel.Icon(Icon.From(Lucide.PanelTop)).TabGroup("showcase", 6).Closeable(false);
        });
    }
}

