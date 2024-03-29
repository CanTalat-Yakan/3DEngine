using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Settings : Frame
{
    public Settings()
    {
        this.InitializeComponent();
    }

    private void RadioButton_Click_Light(object sender, RoutedEventArgs e) =>
        Controller.Theme.Instance.SetRequestedTheme(ElementTheme.Light);

    private void RadioButton_Click_Dark(object sender, RoutedEventArgs e) =>
        Controller.Theme.Instance.SetRequestedTheme(ElementTheme.Dark);

    private void RadioButton_Click_Default(object sender, RoutedEventArgs e) =>
        Controller.Theme.Instance.SetRequestedTheme(ElementTheme.Default);
}
