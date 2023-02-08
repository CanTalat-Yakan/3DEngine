using Engine.Utilities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView
{
    public sealed partial class Main : Frame
    {
        private Controller.Main _mainControl;

        public Main(MainWindow mainWindow)
        {
            this.InitializeComponent();

            _mainControl = new(mainWindow, x_Grid_Main, x_TextBlock_Status_Content, x_TextBlock_StatusIcon_Content);
            _mainControl.ControlPlayer = new(x_AppBarToggleButton_Status_Play, x_AppBarToggleButton_Status_Pause, x_AppBarButton_Status_Forward);

            InitializeInput();
        }

        private void InitializeInput()
        {
            PointerMoved += Input.PointerMoved;
            PointerReleased += Input.PointerReleased;
            KeyUp += Input.KeyUp;
        }

        private void AppBarToggleButton_Status_Play_Click(object sender, RoutedEventArgs e) => _mainControl.ControlPlayer.Play();

        private void AppBarToggleButton_Status_Pause_Click(object sender, RoutedEventArgs e) => _mainControl.ControlPlayer.Pause();

        private void AppBarButton_Status_Forward_Click(object sender, RoutedEventArgs e) => _mainControl.ControlPlayer.Forward();

        private void AppBarButton_Status_Kill_Click(object sender, RoutedEventArgs e) => _mainControl.ControlPlayer.Kill();

        private void AppBarToggleButton_Status_Light(object sender, RoutedEventArgs e) { Controller.Theme.Instance.SetRequstedTheme(); }
    }
}
