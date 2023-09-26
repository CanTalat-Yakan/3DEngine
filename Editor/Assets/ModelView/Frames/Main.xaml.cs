using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Main : Frame
{
    private Controller.Main MainControl;

    public Main(MainWindow mainWindow)
    {
        this.InitializeComponent();

        MainControl = new(mainWindow, x_Grid_Main, x_TextBlock_Status_Content, x_TextBlock_StatusIcon_Content, x_AppBarToggleButton_Status_OpenPane);
        MainControl.PlayerControl = new(x_AppBarToggleButton_Status_Play, x_AppBarToggleButton_Status_Pause, x_AppBarButton_Status_Forward);
    }

    private void AppBarToggleButton_Status_Play_Click(object sender, RoutedEventArgs e) =>
        MainControl.PlayerControl.Play();

    private void AppBarToggleButton_Status_Pause_Click(object sender, RoutedEventArgs e) =>
        MainControl.PlayerControl.Pause();

    private void AppBarButton_Status_Forward_Click(object sender, RoutedEventArgs e) =>
        MainControl.PlayerControl.Forward();

    private void AppBarButton_Status_Kill_Click(object sender, RoutedEventArgs e) =>
        MainControl.PlayerControl.Kill();

    private void AppBarToggleButton_Status_Light(object sender, RoutedEventArgs e) =>
        Controller.Theme.Instance.SetRequstedTheme();
}
