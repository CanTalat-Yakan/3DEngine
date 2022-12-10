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
            if (Instance is null)
                Instance = this;

            MainWindow = mainWindow;
            Content = content;
            Status = status;
            StatusIcon = icon;

            LayoutControl = new Layout(
                Content,
                new ModelView.ViewPort(),
                new ModelView.Hierarchy(),
                new ModelView.Properties(),
                new ModelView.Output(),
                new ModelView.Files());

            LayoutControl.CreateLayout();
        }
    }
}
