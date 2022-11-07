using Microsoft.UI.Xaml.Controls;
using Editor.UserControls;

namespace Editor.Controls
{
    internal class MainController
    {
        public static MainController Instance { get; private set; }

        public LayoutController LayoutControl;
        public PlayerController ControlPlayer;
        public Grid Content;
        public TextBlock Status;
        public MainWindow MainWindow;
        
        public MainController(MainWindow mainWindow, Grid content, TextBlock status)
        {
            if (Instance is null)
                Instance = this;

            MainWindow = mainWindow;
            Content = content;
            Status = status;

            LayoutControl = new LayoutController(
                Content,
                new ViewPort(),
                new Hierarchy(),
                new Properties(),
                new Output(),
                new Files());

            LayoutControl.Initialize();
        }
    }
}
