using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView
{
    public sealed partial class Settings : Frame
    {
        public Settings()
        {
            this.InitializeComponent();
        }

        private void RadioButton_Click_Light(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => 
            Controller.Theme.Instance.SetRequstedTheme(ElementTheme.Light);

        private void RadioButton_Click_Dark(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => 
            Controller.Theme.Instance.SetRequstedTheme(ElementTheme.Dark);

        private void RadioButton_Click_Default(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) => 
            Controller.Theme.Instance.SetRequstedTheme(ElementTheme.Default);
    }
}
