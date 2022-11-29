using Microsoft.UI.Xaml.Controls;
using Editor.UserControls;

namespace Editor.Controller
{
    internal class MainController
    {
        public static MainController Instance { get; private set; }

        public LayoutController LayoutControl;
        public PlayerController ControlPlayer;
        public Grid Content;
        public TextBlock Status;
        public Viewbox StatusIcon;

        public MainController(Grid content, TextBlock status, Viewbox icon)
        {
            if (Instance is null)
                Instance = this;

           Content = content;
            Status = status;
            StatusIcon = icon;

            LayoutControl = new LayoutController(
                Content,
                new ViewPort(),
                new Hierarchy(),
                new Properties(),
                new Output(),
                new Files());

            LayoutControl.CreateLayout();
        }
    }
}
