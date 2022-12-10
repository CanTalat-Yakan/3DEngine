using Microsoft.UI.Xaml.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView
{
    public sealed partial class Properties : UserControl
    {
        public event PropertyChangedEventHandler EventPropertyChanged;

        internal Controller.Properties _propertiesControl;

        public Properties(object content = null)
        {
            this.InitializeComponent();

            _propertiesControl = new Controller.Properties(x_StackPanel_Properties, content);
        }

        private void FirePropertyChanged([CallerMemberName] string memberName = null) => EventPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }
}
