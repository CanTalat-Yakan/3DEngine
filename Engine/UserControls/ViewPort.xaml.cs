using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.UI.Core;
using Engine.Components;
using Engine.Editor;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class ViewPort : UserControl
    {
        internal Engine.Loop engineLoop;

        internal TextBlock debugProfiling;
        internal Grid borderBrush;

        public ViewPort()
        {
            this.InitializeComponent();

            debugProfiling = x_TextBlock_Debug_FPS;
            borderBrush = x_Grid_ViewPort_BorderBrush;

            Loaded += Initialize;
        }

        void Initialize(object sender, RoutedEventArgs e)
        {
            engineLoop = new Engine.Loop(x_SwapChainPanel_ViewPort, debugProfiling);

            PointerPressed += engineLoop.input.PointerPressed;
            PointerWheelChanged += engineLoop.input.PointerWheelChanged;
            PointerReleased += engineLoop.input.PointerReleased;
            PointerMoved += engineLoop.input.PointerMoved;
            KeyDown += engineLoop.input.KeyDown;
            KeyUp += engineLoop.input.KeyUp;

            //var window = (Application.Current as App)?.Window as MainWindow;
            //window.CoreWindow.KeyDown += m_Engine.m_Input.KeyDown;
            //window.CoreWindow.KeyUp += m_Engine.m_Input.KeyUp;
            //window.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            //Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        }


        void x_Slider_FOV_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) { Camera.s_fieldOfView = e.NewValue; }
        void x_NumberBox_CameraSpeed_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) { Controller.s_movementSpeed = (float)args.NewValue; }
    }
}
