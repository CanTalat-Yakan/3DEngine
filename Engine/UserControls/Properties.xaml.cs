using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Editor.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Properties : UserControl
    {
        public event PropertyChangedEventHandler EventPropertyChanged;

        internal PropertiesController _propertiesControl;

        public Properties(object content = null)
        {
            this.InitializeComponent();

            _propertiesControl = new PropertiesController(x_StackPanel_Properties, content);
        }

        private void FirePropertyChanged([CallerMemberName] string memberName = null) => EventPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName));
    }
}
