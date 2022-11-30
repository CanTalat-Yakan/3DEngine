﻿using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Windows.System;
using WinUIEx;
using Editor.Controller;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor
{
    public sealed partial class MainWindow : WindowEx
    {
        internal ThemeController _themeControl;

        private Frames.Main _main;

        public MainWindow()
        {
            this.InitializeComponent();

            ExtendsContentIntoTitleBar = true; // enable custom titlebar

            _themeControl = new ThemeController(this, x_Page_Main);

            _main = new(this);
        }

        private void AppBarButton_Documentation_Click(object sender, RoutedEventArgs e) => _ = Launcher.LaunchUriAsync(new System.Uri(@"https://3DEngine.wiki/"));

        private void x_NavigationView_Main_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.IsSettingsSelected == true)
                x_Frame_Content.Content = new Frames.Settings();

            if (args.SelectedItemContainer != null)
                switch (args.SelectedItemContainer.Tag)
                {
                    case "home":
                        x_Frame_Content.Content = new Frames.Home();
                        break;
                    case "engine":
                        x_Frame_Content.Content = _main;

                        //x_NavigationView_Main.SelectedItem = new Frames.Main();
                        //x_NavigationView_Main.Content = x_Frame_Content;
                        //x_Frame_Content.Content = new Frames.Main();

                        //x_Frame_Content.Navigate(typeof(Frames.Main));
                        break;
                    default:
                        break;
                }
        }
    }
}