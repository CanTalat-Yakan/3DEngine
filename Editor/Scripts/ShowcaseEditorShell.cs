using Editor.Shell;

[EditorShell]
public class ShowcaseEditorShell : IEditorShellBuilder
{
    public int Order => 7;

    public void Build(IShellBuilder shell)
    {
        shell.Panel("showcase-editor", "Editor Components", DockZone.Center, panel =>
        {
            panel.Content(ui =>
            {
                ui.Div(Css.Container().PaddingY(6), container =>
                {
                    container.Div(Css.MarginXAuto().MaxWidth("3xl").SpaceY(6), inner =>
                    {
                        inner.Card(Css.Default, card =>
                        {
                            card.Title("Editor-Specific");
                            card.Description("Components designed specifically for the editor shell.");
                            card.Content(c =>
                            {
                                // Tree view
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Tree View");
                                    section.TreeItem("Scene", icon: Icon.From(Lucide.FolderOpen), expanded: true, children: children =>
                                    {
                                        children.TreeItem("Camera", icon: Icon.From(Lucide.Camera), iconColor: "text-blue-400");
                                        children.TreeItem("Lights", icon: Icon.From(Lucide.FolderOpen), expanded: true, children: lights =>
                                        {
                                            lights.TreeItem("Directional Light", icon: Icon.From(Lucide.Sun), iconColor: "text-yellow-400");
                                            lights.TreeItem("Point Light", icon: Icon.From(Lucide.Lightbulb), iconColor: "text-orange-400");
                                        });
                                        children.TreeItem("Meshes", icon: Icon.From(Lucide.FolderOpen), expanded: true, children: meshes =>
                                        {
                                            meshes.TreeItem("Cube", icon: Icon.From(Lucide.Box), selected: true);
                                            meshes.TreeItem("Sphere", icon: Icon.From(Lucide.Circle));
                                            meshes.TreeItem("Plane", icon: Icon.From(Lucide.Square));
                                        });
                                    });
                                });

                                c.Separator();

                                // Field rows (Inspector style)
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Field Rows (Inspector)");
                                    section.FieldRow("Position X", ctrl => ctrl.NumericInput(Css.Default, value: 0, step: 0.1));
                                    section.FieldRow("Position Y", ctrl => ctrl.NumericInput(Css.Default, value: 1.5, step: 0.1));
                                    section.FieldRow("Position Z", ctrl => ctrl.NumericInput(Css.Default, value: 0, step: 0.1));
                                    section.Separator();
                                    section.FieldRow("Scale", ctrl => ctrl.Slider(Css.Default, value: 1, min: 0.1, max: 10, step: 0.1));
                                    section.FieldRow("Visible", ctrl => ctrl.Checkbox(Css.Default, initial: true));
                                    section.FieldRow("Material", ctrl => ctrl.Select(Css.Default, new[]
                                    {
                                        ("default", "Default"),
                                        ("metal", "Metal"),
                                        ("wood", "Wood"),
                                        ("glass", "Glass")
                                    }, selected: "default"));
                                });

                                c.Separator();

                                // Empty State
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Empty State");
                                    section.EmptyState(
                                        icon: Icon.From(Lucide.Inbox),
                                        title: "No items found",
                                        description: "Try adjusting your search or filter to find what you're looking for.");
                                });

                                c.Separator();

                                // Log Entries
                                c.Div(Css.SpaceY(2), section =>
                                {
                                    section.Label(Css.Default, "Log Entries");
                                    section.LogEntry("10:23:41", "INFO", "Engine", "Application started successfully");
                                    section.LogEntry("10:23:42", "WARN", "Renderer", "Shader compilation took 250ms");
                                    section.LogEntry("10:23:43", "ERROR", "Physics", "Collision mesh missing for entity #42");
                                    section.LogEntry("10:23:44", "INFO", "Assets", "Loaded 128 textures, 64 meshes");
                                });
                            });
                        });
                    });
                });
            });
            panel.Icon(Icon.From(Lucide.Wrench)).TabGroup("showcase", 7).Closeable(false);
        });
    }
}

