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

        internal PropertiesController _properitesControl = new PropertiesController();

        public Properties()
        {
            this.InitializeComponent();

            List<Grid> collection = new List<Grid>();
            collection.Add(_properitesControl.CreateColorButton());
            collection.Add(_properitesControl.CreateNumberInput());
            collection.Add(_properitesControl.CreateTextInput());
            collection.Add(_properitesControl.CreateVec2Input());
            collection.Add(_properitesControl.CreateVec3Input());
            collection.Add(_properitesControl.CreateSlider());
            collection.Add(_properitesControl.CreateBool());
            collection.Add(_properitesControl.CreateTextureSlot());
            collection.Add(_properitesControl.CreateReferenceSlot());
            collection.Add(_properitesControl.CreateHeader());
            collection.Add(_properitesControl.WrapExpander(_properitesControl.CreateEvent()));

            x_StackPanel_Properties.Children.Add(_properitesControl.CreateScript("Example", collection.ToArray()));
            x_StackPanel_Properties.Children.Add(_properitesControl.CreateScript("Another", _properitesControl.CreateSpacer()));
        }

        private void AppBarButton_Click_SelectImagePath(object sender, RoutedEventArgs e) { }//m_Control.SelectImage(Img_SelectTexture, x_TextBlock_TexturePath); }

        private void AppBarButton_Click_SelectFilePath(object sender, RoutedEventArgs e) { }//m_Control.SelectFile(x_TextBlock_FilePath); }

        private void FirePropertyChanged([CallerMemberName] string memberName = null) { EventPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName)); }
    }
}
