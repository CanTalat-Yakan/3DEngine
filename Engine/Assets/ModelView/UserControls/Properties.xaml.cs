using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView
{
    public sealed partial class Properties : UserControl
    {
        internal Controller.Properties _propertiesControl;

        public Properties(object content = null)
        {
            this.InitializeComponent();

            _propertiesControl = new(x_StackPanel_Properties, content);
        }

        private void AppBarButton_SwitchLayout_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
            Controller.Main.Instance.LayoutControl.SwitchPaneLayout();
    }
}
