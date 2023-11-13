using Windows.System;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using WinUIEx;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor;

public sealed partial class MainWindow : WindowEx
{
    internal Controller.Theme ThemeControl;

    public ModelView.Main Main { get => _main is not null ? _main : _main = new(this); }
    private ModelView.Main _main;

    public MainWindow()
    {
        this.InitializeComponent();

        ExtendsContentIntoTitleBar = true; // enable custom title bar

        ThemeControl = new(this, x_Page_Main);
    }

    private void AppBarButton_Help_Click(object sender, RoutedEventArgs e) =>
        _ = Launcher.LaunchUriAsync(new System.Uri(@"https://engine3d.gitbook.io/wiki"));

    private void x_NavigationView_Main_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected == true)
        {
            x_Frame_Content.Content = new ModelView.Settings();
            x_NavigationView_Main.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
        }

        if (args.SelectedItemContainer is not null)
            switch (args.SelectedItemContainer.Tag)
            {
                case "home":
                    x_Frame_Content.Content = new ModelView.Home(this, x_NavigationView_Main);
                    x_NavigationView_Main.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    break;
                case "wiki":
                    x_Frame_Content.Content = new ModelView.Wiki();
                    x_NavigationView_Main.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    break;
                case "engine":
                    x_Frame_Content.Content = Main;
                    x_NavigationView_Main.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    break;
                default:
                    break;
            }
    }
}
