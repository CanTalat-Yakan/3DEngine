using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Editor.Controls;
using Engine.Components;
using Engine.Editor;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class ViewPort : UserControl
    {
        internal Engine.Core _engineCore;
        internal ViewPortController _viewPortControl;

        public ViewPort()
        {
            this.InitializeComponent();

            _viewPortControl = new ViewPortController(this, x_Grid_ViewPort_Main);

            Loaded += Initialize;
        }

        private void Initialize(object sender, RoutedEventArgs e)
        {
            _engineCore = new Engine.Core(x_SwapChainPanel_ViewPort, _viewPortControl.Profile.Text);

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
    }
}
