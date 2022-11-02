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
        public Engine.Core EngineCore;

        public TextBlock DebugProfiling;
        public Grid BorderColor;

        public ViewPort()
        {
            this.InitializeComponent();

            DebugProfiling = x_TextBlock_Debug_FPS;
            BorderColor = x_Grid_ViewPort_BorderBrush;

            Loaded += Initialize;
        }

        private void Initialize(object sender, RoutedEventArgs e)
        {
            EngineCore = new Engine.Core(x_SwapChainPanel_ViewPort, DebugProfiling);

            PointerPressed += EngineCore.Input.PointerPressed;
            PointerWheelChanged += EngineCore.Input.PointerWheelChanged;
            PointerReleased += EngineCore.Input.PointerReleased;
            PointerMoved += EngineCore.Input.PointerMoved;
            KeyDown += EngineCore.Input.KeyDown;
            KeyUp += EngineCore.Input.KeyUp;

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
