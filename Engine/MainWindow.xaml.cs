using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.System;
using WinUIEx;
using Engine.Utilities;
using Vortice.DirectInput;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor
{
    public sealed partial class MainWindow : WindowEx
    {
        internal Controller.Theme _themeControl;

        private ModelView.Main _main;
        public ModelView.Main Main { get => _main != null ? _main : _main = new(this); }

        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true; // enable custom titlebar

            _themeControl = new(this, x_Page_Main);
        }

        private void AppBarButton_Help_Click(object sender, RoutedEventArgs e) => _ = Launcher.LaunchUriAsync(new System.Uri(@"https://3DEngine.wiki/"));

        private void x_NavigationView_Main_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
            {
                x_Frame_Content.Content = new ModelView.Settings();
                x_NavigationView_Main.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
            }

            if (args.SelectedItemContainer != null)
                switch (args.SelectedItemContainer.Tag)
                {
                    case "home":
                        x_Frame_Content.Content = new ModelView.Home(this, x_NavigationView_Main);
                        x_NavigationView_Main.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                        break;
                    case "documentation":
                        x_Frame_Content.Content = new ModelView.Documentation();
                        x_NavigationView_Main.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                        break;
                    case "engine":
                        x_Frame_Content.Content = Main;
                        x_NavigationView_Main.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                        break;
                    default:
                        break;
                }
        }
    }
}