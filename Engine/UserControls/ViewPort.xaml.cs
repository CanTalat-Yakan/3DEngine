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
using WinUI3DEngine.Assets.Engine;
using WinUI3DEngine.Assets.Engine.Components;
using WinUI3DEngine.Assets.Engine.Editor;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Engine.UserControls
{
    public sealed partial class ViewPort : UserControl
    {
        internal CEngine m_Engine;

        internal TextBlock m_DebugProfiling;
        internal Grid m_BorderBrush;

        public ViewPort()
        {
            this.InitializeComponent();

            m_DebugProfiling = x_TextBlock_Debug_FPS;
            m_BorderBrush = x_Grid_ViewPort_BorderBrush;

            Loaded += Initialize;
        }

        void Initialize(object sender, RoutedEventArgs e)
        {
            m_Engine = new CEngine(x_SwapChainPanel_ViewPort, m_DebugProfiling);

            PointerPressed += m_Engine.m_Input.PointerPressed;
            PointerWheelChanged += m_Engine.m_Input.PointerWheelChanged;
            PointerReleased += m_Engine.m_Input.PointerReleased;
            PointerMoved += m_Engine.m_Input.PointerMoved;
            KeyDown += m_Engine.m_Input.KeyDown;
            KeyUp += m_Engine.m_Input.KeyUp;

            //var window = (Application.Current as App)?.Window as MainWindow;
            //window.CoreWindow.KeyDown += m_Engine.m_Input.KeyDown;
            //window.CoreWindow.KeyUp += m_Engine.m_Input.KeyUp;
            //window.CoreWindow.GetKeyState(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
            //Microsoft.UI.Input.InputKeyboardSource.GetKeyStateForCurrentThread(Windows.System.VirtualKey.Control).HasFlag(CoreVirtualKeyStates.Down);
        }


        void x_Slider_FOV_ValueChanged(object sender, RangeBaseValueChangedEventArgs e) { CCamera.m_FOV = e.NewValue; }
        void x_NumberBox_CameraSpeed_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args) { CController.m_MovementSpeed = (float)args.NewValue; }
    }
}
