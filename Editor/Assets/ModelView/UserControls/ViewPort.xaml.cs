using Microsoft.UI.Input;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using SharpGen.Runtime;
using System;
using WinUIEx;

using Editor.Controller;
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

        InitializeRenderer(new());

        var hWnd = (Application.Current as App)?.Window.GetWindowHandle();
        _engineCore = new Engine.Core(_renderer, hWnd.Value, Controller.Files.AssetsPath);
        //Unloaded += (s, e) => _engineCore.Dispose();

        _engineCore.Renderer.Data.SetVsync(false);
        _engineCore.Renderer.Data.SetSuperSample(true);

        _viewportControl = new Controller.Viewport(this, x_Grid_Overlay);

        // Adds an event handler for the CompositionTarget.Rendering event,
        // which is triggered when the composition system is rendering a frame.
        // The code inside the event handler will be executed each time the event is raised.
        CompositionTarget.Rendering += (s, e) =>
        {
            if (_engineCore.Renderer is null)
                return;

            _engineCore.SetPlayMode(Controller.Main.Instance.PlayerControl.PlayMode == PlayMode.Playing);
            _engineCore.SetPlayModeStarted(Controller.Main.Instance.PlayerControl.CheckPlayModeStarted());

            _engineCore.Frame();

            _viewportControl.Profile.Text = Engine.Profiler.GetString();

            Binding.Update();
            Controller.Output.Log(Engine.Output.DequeueLog());
        };

        PointerEntered += (s, e) => Engine.Editor.ViewportController.ViewportFocused = true;
        PointerExited += (s, e) => Engine.Editor.ViewportController.ViewportFocused = false;

        //var arrow = InputSystemCursor.Create(InputSystemCursorShape.Arrow);
        var cross = InputSystemCursor.Create(InputSystemCursorShape.Cross);
        x_CustomCursorGrid.InputCursor = cross;
    }

    private void InitializeRenderer(Renderer renderer)
    {
        _renderer = renderer;

        // Gets the native object for the SwapChainPanel control.
        using (var nativeObject = ComObject.As<Vortice.WinUI.ISwapChainPanelNative2>(x_SwapChainPanel_Viewport))
        {
            var result = nativeObject.SetSwapChain(_renderer.SwapChain);
            if (result.Failure)
                throw new Exception(result.Description);
        }
    }

    private void x_SwapChainPanel_Viewport_SizeChanged(object sender, SizeChangedEventArgs e) =>
        // Register an event handler for the SizeChanged event of the SwapChainPanel. This will be used to handle any changes in the size of the panel.
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