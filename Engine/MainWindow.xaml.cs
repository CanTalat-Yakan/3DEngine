using Editor.Controls;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using System;
using System.IO;
using WinRT.Interop;
using Windows.UI.ViewManagement;
using WinUIEx;
using System.Runtime.InteropServices;
using WinRT;

namespace Editor
{
    public sealed partial class MainWindow : WindowEx
    {
        MainController m_mainControl;

        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true;

            m_mainControl = new MainController(x_Grid_Main, x_TextBlock_Status_Content);
            m_mainControl.m_Player = new PlayerController(x_AppBarToggleButton_Status_Play, x_AppBarToggleButton_Status_Pause, x_AppBarButton_Status_Forward);
        }

        private void AppBarToggleButton_Status_Play_Click(object sender, RoutedEventArgs e) { m_mainControl.m_Player.Play(); }
        private void AppBarToggleButton_Status_Pause_Click(object sender, RoutedEventArgs e) { m_mainControl.m_Player.Pause(); }
        private void AppBarButton_Status_Forward_Click(object sender, RoutedEventArgs e) { m_mainControl.m_Player.Forward(); }
        private void AppBarButton_Status_Kill_Click(object sender, RoutedEventArgs e) { m_mainControl.m_Player.Kill(); }
        private void AppBarToggleButton_Status_Light(object sender, RoutedEventArgs e) { x_Frame_Main.RequestedTheme = x_Frame_Main.RequestedTheme == ElementTheme.Light ? ElementTheme.Dark : x_Frame_Main.RequestedTheme = ElementTheme.Light; }
    }
}