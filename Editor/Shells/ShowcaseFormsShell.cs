using Editor.Shell;

/// <summary>
/// Showcase shell demonstrating form components: inputs, textarea, select, combobox,
/// checkbox, radio group, switch, slider, toggle, and toggle group.
/// </summary>
/// <remarks>
/// Registered at route <c>/showcase/forms</c> as a center-docked tabbed panel
/// (tab group <c>"showcase"</c>, index 2).
/// </remarks>
/// <seealso cref="ShowcasePageHelper"/>
/// <seealso cref="IEditorShellBuilder"/>
[EditorShell]
public class ShowcaseFormsShell : IEditorShellBuilder
{
    /// <inheritdoc />
    public int Order => 2;

    /// <inheritdoc />
    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-forms", "Forms", DockZone.Center, panel =>
        {
            panel.Icon(Icon.From(Lucide.TextCursorInput)).TabGroup("showcase", 2).Closeable(false).Route("/showcase/forms");
            panel.Content(ui =>
            {
                ShowcasePageHelper.WrapWithSidebar(ui, content =>
                {
                    content.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(6), inner =>
                    {
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Forms");
                            card.Description("Form components for collecting user input.");
                            card.Content(c =>
                            {
                                // Input
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Input");
                                    section.Input(Css.Default, placeholder: "Enter your email...");
                                });

                                c.Separator();

                                // Textarea
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Textarea");
                                    section.Textarea(Css.Default, placeholder: "Tell us about yourself...");
                                });

                                c.Separator();

                                // Select
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Select");
                                    section.Select(Css.Default, new[]
                                    {
                                        ("blazor", "Blazor"),
                                        ("react", "React"),
                                        ("vue", "Vue"),
                                        ("angular", "Angular"),
                                        ("svelte", "Svelte")
                                    }, placeholder: "Choose a framework");
                                });

                                c.Separator();

                                // Combobox
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Combobox");
                                    section.Combobox(Css.Default, new[]
                                    {
                                        ("us", "United States"),
                                        ("gb", "United Kingdom"),
                                        ("ca", "Canada"),
                                        ("au", "Australia"),
                                        ("de", "Germany")
                                    }, placeholder: "Select a country...");
                                });

                                c.Separator();

                                // Checkbox
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Checkbox");
                                    section.Checkbox(Css.Default, "Accept terms and conditions", initial: false);
                                    section.Checkbox(Css.Default, "Receive marketing emails", initial: true);
                                });

                                c.Separator();

                                // Radio Group
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Radio Group");
                                    section.RadioGroup(Css.Default, new[]
                                    {
                                        ("free", "Free"),
                                        ("pro", "Pro - $9/month"),
                                        ("enterprise", "Enterprise - Contact us")
                                    }, selected: "free");
                                });

                                c.Separator();

                                // Switch
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Switch");
                                    section.Switch(Css.Default, "Enable notifications", initial: true);
                                    section.Switch(Css.Default, "Dark mode", initial: false);
                                });

                                c.Separator();

                                // Slider
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Slider");
                                    section.Slider(Css.Default, value: 50, min: 0, max: 100, step: 1);
                                });

                                c.Separator();

                                // Toggle & ToggleGroup
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Toggle");
                                    section.Row(Css.Default, row =>
                                    {
                                        row.Toggle(Css.Default, "Bold", icon: Icon.From(Lucide.Bold));
                                        row.Toggle(Css.Default, "Italic", icon: Icon.From(Lucide.Italic));
                                        row.Toggle(Css.Default, "Underline", icon: Icon.From(Lucide.Underline));
                                    });
                                });

                                c.Separator();

                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Toggle Group");
                                    section.ToggleGroup(Css.Default, new[]
                                    {
                                        ("left", "Left"),
                                        ("center", "Center"),
                                        ("right", "Right")
                                    }, selected: "center");
                                });
                            });
                        });
                    });
                });
            });
        });
    }
}
