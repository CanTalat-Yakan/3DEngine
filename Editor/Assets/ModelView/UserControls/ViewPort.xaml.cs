using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class ViewPort : UserControl
{
    internal Engine.Core _engineCore;
    internal Controller.ViewPort _viewPortControl;

    public ViewPort()
    {
        this.InitializeComponent();

        Loaded += (s, e) => _engineCore = new Engine.Core(x_SwapChainPanel_ViewPort);
        Unloaded += (s, e) => _engineCore.Renderer.Dispose();

        _viewPortControl = new Controller.ViewPort(this, x_Grid_Overlay);

        // Adds an event handler for the CompositionTarget.Rendering event,
        // which is triggered when the composition system is rendering a frame.
        // The code inside the event handler will be executed each time the event is raised.
        CompositionTarget.Rendering += (s, e) => _engineCore.Frame();
        CompositionTarget.Rendering += (s, e) => _viewPortControl.Profile.Text = Profiler.GetString();

        InitializeInput();
    }

    private void InitializeInput()
    {
        PointerPressed += Input.PointerPressed;
        PointerWheelChanged += Input.PointerWheelChanged;
        KeyDown += Input.KeyDown;
    }
}
