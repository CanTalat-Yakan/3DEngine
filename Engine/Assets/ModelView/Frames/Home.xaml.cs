using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Editor;
using Editor.Controller;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Frames
{
    public sealed partial class Home : Frame
    {
        public Home()
        {
            this.InitializeComponent();
        }

        private void AppBarToggleButton_Status_Light(object sender, RoutedEventArgs e) { MainController.Instance.MainWindow._themeControl.SetRequstedTheme(); }
    }
}
