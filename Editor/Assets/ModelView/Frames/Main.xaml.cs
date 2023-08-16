using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Main : Frame
{
    private Controller.Main _mainControl;

    public Main(MainWindow mainWindow)
    {
        this.InitializeComponent();

        _mainControl = new(mainWindow, x_Grid_Main, x_TextBlock_Status_Content, x_TextBlock_StatusIcon_Content, x_AppBarToggleButton_Status_OpenPane);
        _mainControl.PlayerControl = new(x_AppBarToggleButton_Status_Play, x_AppBarToggleButton_Status_Pause, x_AppBarButton_Status_Forward);

        InitializeInput();
    }

    private void InitializeInput()
    {
        PointerMoved += Engine.Utilities.Input.PointerMoved;
        PointerReleased += Input.PointerReleased;
        KeyUp += Input.KeyUp;
    }

    private void AppBarToggleButton_Status_Play_Click(object sender, RoutedEventArgs e) =>
        _mainControl.PlayerControl.Play();

    private void AppBarToggleButton_Status_Pause_Click(object sender, RoutedEventArgs e) =>
        _mainControl.PlayerControl.Pause();

    private void AppBarButton_Status_Forward_Click(object sender, RoutedEventArgs e) =>
        _mainControl.PlayerControl.Forward();

    private void AppBarButton_Status_Kill_Click(object sender, RoutedEventArgs e) =>
        _mainControl.PlayerControl.Kill();

    private void AppBarToggleButton_Status_Light(object sender, RoutedEventArgs e) =>
        Controller.Theme.Instance.SetRequstedTheme();
}
