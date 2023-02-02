using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView
{
    public sealed partial class ViewPort : UserControl
    {
        internal Engine.Core _engineCore;
        internal Controller.ViewPort _viewPortControl;

        public ViewPort()
        {
            this.InitializeComponent();

            Loaded += (s, e) => _engineCore = new Engine.Core(x_SwapChainPanel_ViewPort, _viewPortControl.Profile);
            Loaded += (s, e) => InitializeInput();

            _viewPortControl = new Controller.ViewPort(this, x_Grid_Overlay);
        }

        private void InitializeInput()
        {
            PointerPressed += _engineCore.Input.PointerPressed;
            PointerWheelChanged += _engineCore.Input.PointerWheelChanged;
            PointerReleased += _engineCore.Input.PointerReleased;
            PointerMoved += _engineCore.Input.PointerMoved;
            KeyDown += _engineCore.Input.KeyDown;
            KeyUp += _engineCore.Input.KeyUp;
        }
    }
}
