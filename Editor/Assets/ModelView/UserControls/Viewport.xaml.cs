using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

using Engine.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Viewport : UserControl
{
    internal Engine.Core _engineCore;
    internal Renderer _renderer;

    internal Controller.Viewport _viewportControl;

    public Viewport()
    {
        this.InitializeComponent();

        _viewportControl = new Controller.Viewport(this, x_Grid_Overlay);
        _viewportControl.InitializeRenderer(out _renderer, x_SwapChainPanel_Viewport);
        _viewportControl.InitializeEngineCore(_renderer, out _engineCore);

        // Adds an event handler for the CompositionTarget.Rendering event,
        // which is triggered when the composition system is rendering a frame.
        // The code inside the event handler will be executed each time the event is raised.
        CompositionTarget.Rendering += (s, e) => _engineCore.Frame();

        PointerEntered += (s, e) => Engine.Editor.ViewportController.ViewportFocused = true;
        PointerExited += (s, e) => Engine.Editor.ViewportController.ViewportFocused = false;

        var cross = InputSystemCursor.Create(InputSystemCursorShape.Cross);
        x_CustomCursorGrid.InputCursor = cross;
    }

    private void x_SwapChainPanel_Viewport_SizeChanged(object sender, SizeChangedEventArgs e) =>
        // Register an event handler for the SizeChanged event of the SwapChainPanel.
        // This will be used to handle any changes in the size of the panel.
        _renderer.Resize(
            (int)e.NewSize.Width,
            (int)e.NewSize.Height);
}

public class CustomCursorGrid : Grid
{
    public InputCursor InputCursor
    {
        get => ProtectedCursor;
        set => ProtectedCursor = value;
    }
}