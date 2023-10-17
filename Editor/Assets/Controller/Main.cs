using Microsoft.UI.Xaml.Controls;

namespace Editor.Controller;

internal sealed class Main
{
    public static Main Instance { get; private set; }

    public Layout LayoutControl;
    public Player PlayerControl;
    public Grid Content;
    public TextBlock Status;
    public Viewbox StatusIcon;
    public MainWindow MainWindow;
    public AppBarToggleButton OpenPane;

    public Main(MainWindow mainWindow, Grid content, TextBlock status, Viewbox icon, AppBarToggleButton openPane)
    {
        // Set the singleton instance of the class, if it hasn't been already.
        Instance ??= this;

        // Assign local variables.
        MainWindow = mainWindow;
        Content = content;
        Status = status;
        StatusIcon = icon;
        OpenPane = openPane;

        // Create a new layout control and pass in the content and views.
        LayoutControl = new Layout(
            Content,
            new ModelView.Viewport(),
            new ModelView.Hierarchy(),
            new ModelView.Properties(),
            new ModelView.Output(),
            new ModelView.Files());

        // Call the CreateLayout method to create the layout.
        LayoutControl.CreateLayout();
    }
}
