using Microsoft.UI.Xaml;
using System.Diagnostics;
using WinUIEx;
using Editor.Controller;
using System;

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

            _themeControl = new ThemeController(this, x_Page_Main);
            _mainControl = new MainController(this, x_Grid_Main, x_TextBlock_Status_Content, x_TextBlock_StatusIcon_Content);
            _mainControl.ControlPlayer = new PlayerController(x_AppBarToggleButton_Status_Play, x_AppBarToggleButton_Status_Pause, x_AppBarButton_Status_Forward);
        }

        private void AppBarToggleButton_Status_Play_Click(object sender, RoutedEventArgs e) => _mainControl.ControlPlayer.Play();

        private void AppBarToggleButton_Status_Pause_Click(object sender, RoutedEventArgs e) => _mainControl.ControlPlayer.Pause();

        private void AppBarButton_Status_Forward_Click(object sender, RoutedEventArgs e) => _mainControl.ControlPlayer.Forward();

        private void AppBarButton_Status_Kill_Click(object sender, RoutedEventArgs e) => _mainControl.ControlPlayer.Kill();

        private void AppBarToggleButton_Status_Light(object sender, RoutedEventArgs e) => _themeControl.SetRequstedTheme();

        private void AppBarButton_Documentation_Click(object sender, RoutedEventArgs e) => _ = Windows.System.Launcher.LaunchUriAsync(new System.Uri(@"https://3DEngine.wiki/"));
    }
}