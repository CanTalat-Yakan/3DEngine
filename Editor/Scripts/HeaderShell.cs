using Editor.Shell;

[EditorShell]
public class HeaderShell : IEditorShellBuilder
{
    public int Order => -1;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("header", "Header", DockZone.Top, header =>
        {
            header.Content(ui =>
            {
                ui.Div("sticky top-0 z-50 w-full border-b bg-background", outer =>
                {
                    outer.Div(Css.Container().Flex().Height("14").Items(Align.Center), bar =>
                    {
                        // Brand
                        bar.Div("mr-4 flex", brand =>
                        {
                            brand.Div("mr-6 flex items-center space-x-2", link =>
                            {
                                link.Icon("text-primary", Icon.From(Lucide.Box), 24);
                                link.Text(Css.FontBold(), "BlazorBlueprint");
                            });
                        });

                        // Nav links
                        bar.Div(Css.Flex().Items(Align.Center).Raw("space-x-6 text-sm font-medium"), nav =>
                        {
                            nav.Button(
                                "transition-colors hover:text-foreground/80 text-foreground/60",
                                "Home", href: "/", variant: Variant.From(ButtonStyle.Link));
                            nav.Button(
                                "transition-colors hover:text-foreground/80 text-foreground/60",
                                "Showcase", href: "/showcase/buttons", variant: Variant.From(ButtonStyle.Link));
                        });

                        // Right side — dark mode toggle
                        bar.Div(Css.Flex().Flex1().Items(Align.Center).Justify(Justify.End).Raw("space-x-2"), actions =>
                        {
                            actions.DarkModeToggle();
                        });
                    });
                });
            });
            header.Closeable(false);
        });
    }
}

