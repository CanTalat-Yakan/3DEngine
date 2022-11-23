﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System;
using Editor.Controller;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Output : UserControl
    {
        public Grid ChangeColorWithTheme;

        internal OutputController _outputControl;

        public Output()
        {
            this.InitializeComponent();

            ChangeColorWithTheme = x_Grid_Main;

            _outputControl = new OutputController(
                x_Stackpanel_Output, 
                x_ScrollViewer_Output, 
                x_AppBarToggleButton_Output_Collapse, 
                x_AppBarToggleButton_Filter_Messages, 
                x_AppBarToggleButton_Filter_Warnings, 
                x_AppBarToggleButton_Filter_Errors, 
                x_AppBarToggleButton_Debug_ErrorPause, 
                x_AppBarToggleButton_Debug_ClearPlay);

            DispatcherTimer dispatcher = new DispatcherTimer();
            dispatcher.Interval = TimeSpan.FromMilliseconds(42);
            dispatcher.Tick += Tick;
            //dispatcher.Start();
            DispatcherTimer dispatcherSec = new DispatcherTimer();
            dispatcherSec.Interval = TimeSpan.FromSeconds(1);
            dispatcherSec.Tick += TickSec;
            dispatcherSec.Start();
        }

        private void Tick(object sender, object e)
        {
            if (MainController.Instance.ControlPlayer.PlayMode == EPlayMode.PLAYING)
                Update();
        }

        private void TickSec(object sender, object e)
        {
            if (MainController.Instance.ControlPlayer.PlayMode == EPlayMode.PLAYING)
                UpdateSec();
        }

        private void Update()
        {
            OutputController.Log("Updated Frame..");
        }

        private void UpdateSec()
        {
            ExampleSkriptDebugTest();
        }

        private void ExampleSkriptDebugTest()
        {
            Random rnd = new Random();
            int i = rnd.Next(0, 24);

            OutputController.Log(i.ToString());
            if (i < 5)
                OutputController.Log("Error Example!", EMessageType.ERROR);
            else if (i < 10 && i > 5)
                OutputController.Log("A Warning.", EMessageType.WARNING);
            else if (i < 15)
                OutputController.Log("This is a Message");
            else if (i > 15)
                Test();
        }

        private void Test()
        {
            OutputController.Log("Test");
        }

        private void AppBarButton_Output_Clear(object sender, RoutedEventArgs e) => _outputControl.ClearOutput();

        private void AppBarToggleButton_Output_Collapse_Click(object sender, RoutedEventArgs e) => OutputController.IterateOutputMessages();
        
        private void AppBarToggleButton_Filter_Click(object sender, RoutedEventArgs e) => OutputController.IterateOutputMessages();

        private void AppBarToggleButton_Debug_ErrorPause_Click(object sender, RoutedEventArgs e) { }

        private void AppBarToggleButton_Debug_ClearPlay_Click(object sender, RoutedEventArgs e) { }
    }
}
