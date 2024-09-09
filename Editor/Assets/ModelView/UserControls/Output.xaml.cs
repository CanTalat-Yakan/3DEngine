using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Output : UserControl
{
    public Grid ChangeColorWithTheme;

    internal Controller.Output OutputControl;

    public Output()
    {
        this.InitializeComponent();

        ChangeColorWithTheme = x_Grid_Main;

        OutputControl = new(
            x_Stackpanel_Output,
            x_ScrollViewer_Output,
            x_AppBarToggleButton_Output_Collapse,
            x_AppBarToggleButton_Filter_Messages,
            x_AppBarToggleButton_Filter_Warnings,
            x_AppBarToggleButton_Filter_Errors,
            x_AppBarToggleButton_Debug_ErrorPause,
            x_AppBarToggleButton_Debug_ClearPlay);
    }

    private void AppBarButton_Output_Clear(object sender, RoutedEventArgs e) =>
        OutputControl.ClearOutput();

    private void AppBarToggleButton_Output_Collapse_Click(object sender, RoutedEventArgs e) =>
        Controller.Output.IterateOutputMessages();

    private void AppBarToggleButton_Filter_Click(object sender, RoutedEventArgs e) =>
        Controller.Output.IterateOutputMessages();
}
