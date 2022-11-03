using Editor.Controls;
using Microsoft.UI.Xaml;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor
{
    public sealed partial class MainWindow : WindowEx
    {
        private MainController _mainControl;
        private ThemeController _themeControl;

        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true; // enable custom titlebar

            _themeControl = new ThemeController(x_Frame_Main, this);
            _mainControl = new MainController(x_Grid_Main, x_TextBlock_Status_Content, x_NavigationView);
            _mainControl.ControlPlayer = new PlayerController(x_AppBarToggleButton_Status_Play, x_AppBarToggleButton_Status_Pause, x_AppBarButton_Status_Forward);
        }

        private void AppBarToggleButton_Status_Play_Click(object sender, RoutedEventArgs e) { _mainControl.ControlPlayer.Play(); }

        private void AppBarToggleButton_Status_Pause_Click(object sender, RoutedEventArgs e) { _mainControl.ControlPlayer.Pause(); }

        private void AppBarButton_Status_Forward_Click(object sender, RoutedEventArgs e) { _mainControl.ControlPlayer.Forward(); }

        private void AppBarButton_Status_Kill_Click(object sender, RoutedEventArgs e) { _mainControl.ControlPlayer.Kill(); }

        private void AppBarToggleButton_Status_Light(object sender, RoutedEventArgs e) { _themeControl.SetRequstedTheme(); }
    }
}