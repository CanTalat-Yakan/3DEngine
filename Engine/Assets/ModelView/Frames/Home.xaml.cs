using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Home : Frame
{
    private Controller.Home _homeControl;

    public Home(MainWindow mainWindow, NavigationView navigationView)
    {
        this.InitializeComponent();

        x_TextBlock_AppVersion.Text = Controller.Helper.GetAppVersion();

        _homeControl = new(this, x_StackPanel_Projects, navigationView);
    }

    private void AppBarToggleButton_Status_Light(object sender, RoutedEventArgs e) =>
        Theme.Instance.SetRequstedTheme();

    private void AppBarButton_Click_OpenFolder(object sender, RoutedEventArgs e) =>
        _homeControl.OpenFolder();
}
