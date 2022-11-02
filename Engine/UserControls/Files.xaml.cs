using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Files : UserControl
    {
        public Grid ChangeColorWithTheme;

        public Files()
        {
            this.InitializeComponent();

            ChangeColorWithTheme = x_Grid_ChangeColorWithTheme;
        }
    }
}
