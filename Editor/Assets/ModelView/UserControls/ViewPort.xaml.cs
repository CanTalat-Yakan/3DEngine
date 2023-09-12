using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using SharpGen.Runtime;
using System;
using WinUIEx;
using Engine.Utilities;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class ViewPort : UserControl
{
    internal Engine.Core _engineCore;
    internal Renderer _renderer;

    internal Controller.ViewPort _viewPortControl;

    public ViewPort()
    {
        this.InitializeComponent();

        InitializeRenderer();

        _viewPortControl = new Controller.ViewPort(this, x_Grid_Overlay);

        var hWnd = (Application.Current as App)?.Window.GetWindowHandle();

        Loaded += (s, e) => _engineCore = new Engine.Core(_renderer, hWnd.Value, Controller.Files.AssetsPath);
        Unloaded += (s, e) => _engineCore.Dispose();

        // Adds an event handler for the CompositionTarget.Rendering event,
        // which is triggered when the composition system is rendering a frame.
        // The code inside the event handler will be executed each time the event is raised.
        CompositionTarget.Rendering += (s, e) => _engineCore.Frame();
        CompositionTarget.Rendering += (s, e) => _viewPortControl.Profile.Text = Engine.Profiler.GetString();
        CompositionTarget.Rendering += (s, e) => Controller.Output.Log(Engine.Output.DequeueLog());

        // Register an event handler for the SizeChanged event of the SwapChainPanel. This will be used to handle any changes in the size of the panel.
        x_SwapChainPanel_ViewPort.SizeChanged += (s, e) => _renderer.OnSwapChainSizeChanged((int)e.NewSize.Width, (int)e.NewSize.Height);
    }

    private void InitializeRenderer()
    {
        _renderer = new();

        var result = _renderer.Initialization();
        if (result.Failure)
            throw new Exception(result.Description);

        // Gets the native object for the SwapChainPanel control.
        using (var nativeObject = ComObject.As<Vortice.WinUI.ISwapChainPanelNative2>(x_SwapChainPanel_ViewPort))
            result = nativeObject.SetSwapChain(_renderer.SwapChain);
        if (result.Failure)
            throw new Exception(result.Description);
    }
}
