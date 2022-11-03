using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Engine.Components;
using Engine.Editor;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class ViewPort : UserControl
    {
        public TextBlock DebugProfiling;
        public Grid BorderColor;

        internal Engine.Core _engineCore;

        public ViewPort()
        {
            this.InitializeComponent();

            DebugProfiling = x_TextBlock_Debug_FPS;
            BorderColor = x_Grid_ViewPort_BorderBrush;

            Loaded += Initialize;
        }

        private void Initialize(object sender, RoutedEventArgs e)
        {
            _engineCore = new Engine.Core(x_SwapChainPanel_ViewPort, DebugProfiling);

            PointerPressed += _engineCore.Input.PointerPressed;
            PointerWheelChanged += _engineCore.Input.PointerWheelChanged;
            PointerReleased += _engineCore.Input.PointerReleased;
            PointerMoved += _engineCore.Input.PointerMoved;
            KeyDown += _engineCore.Input.KeyDown;
            KeyUp += _engineCore.Input.KeyUp;

            //var window = (Application.Current as App)?.Window as MainWindow;
            //window.CoreWindow.KeyDown += m_Engine.m_Input.KeyDown;
            //window.CoreWindow.KeyUp += m_Engine.m_Input.KeyUp;
            //window.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            //Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        }

        private void x_Slider_FOV_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) { CameraComponent.s_FieldOfView = e.NewValue; }

        private void x_NumberBox_CameraSpeed_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) { ViewPortController.s_MovementSpeed = (float)args.NewValue; }
    }
}
