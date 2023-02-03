﻿using Engine.Utilities;
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

            InitializeInput();

            Loaded += (s, e) => _engineCore = new Engine.Core(x_SwapChainPanel_ViewPort, _viewPortControl.Profile);
            Unloaded += (s, e) => _engineCore.Renderer.Dispose();

            _viewPortControl = new Controller.ViewPort(this, x_Grid_Overlay);
        }

        private void InitializeInput()
        {
            PointerPressed += Input.PointerPressed;
            PointerWheelChanged += Input.PointerWheelChanged;
            PointerReleased += Input.PointerReleased;
            PointerMoved += Input.PointerMoved;
            KeyDown += Input.KeyDown;
            KeyUp += Input.KeyUp;
        }
    }
}
