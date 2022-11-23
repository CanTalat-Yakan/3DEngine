using Microsoft.UI.Xaml.Controls;
using Editor.Controls;

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

            _viewPortControl = new ViewPortController(this, x_Grid_Main);

            Loaded += (s, e) => _engineCore = new Engine.Core(x_SwapChainPanel_ViewPort, _viewPortControl.Profile.Text);
            Loaded += (s, e) => InitializeInput();
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
