using Microsoft.UI.Xaml.Controls;

namespace Editor.Controller
{
    internal class Main
    {
        public static Main Instance { get; private set; }

        public Layout LayoutControl;
        public Player ControlPlayer;
        public Grid Content;
        public TextBlock Status;
        public Viewbox StatusIcon;
        public MainWindow MainWindow;

        public Main(MainWindow mainWindow, Grid content, TextBlock status, Viewbox icon)
        {
            // Initializes the singleton instance of the class, if it hasn't been already.
            if (Instance is null)
                Instance = this;
            
            // Assign local variables.
            MainWindow = mainWindow;
            Content = content;
            Status = status;
            StatusIcon = icon;

            // Create a new layout control and pass in the content and views.
            LayoutControl = new Layout(
                Content,
                new ModelView.ViewPort(),
                new ModelView.Hierarchy(),
                new ModelView.Properties(),
                new ModelView.Output(),
                new ModelView.Files());

            // Call the CreateLayout method to create the layout.
            LayoutControl.CreateLayout();
}
    }
}
