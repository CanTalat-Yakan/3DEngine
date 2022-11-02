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
        public event PropertyChangedEventHandler _EventPropertyChanged;

        public PropertiesController ProperitesControl = new PropertiesController();

        public Properties()
        {
            this.InitializeComponent();

            List<Grid> collection = new List<Grid>();
            collection.Add(ProperitesControl.CreateColorButton());
            collection.Add(ProperitesControl.CreateNumberInput());
            collection.Add(ProperitesControl.CreateTextInput());
            collection.Add(ProperitesControl.CreateVec2Input());
            collection.Add(ProperitesControl.CreateVec3Input());
            collection.Add(ProperitesControl.CreateSlider());
            collection.Add(ProperitesControl.CreateBool());
            collection.Add(ProperitesControl.CreateTextureSlot());
            collection.Add(ProperitesControl.CreateReferenceSlot());
            collection.Add(ProperitesControl.CreateHeader());
            collection.Add(ProperitesControl.WrapExpander(ProperitesControl.CreateEvent()));

            x_StackPanel_Properties.Children.Add(ProperitesControl.CreateScript("Example", collection.ToArray()));
            x_StackPanel_Properties.Children.Add(ProperitesControl.CreateScript("Another", ProperitesControl.CreateSpacer()));
        }

        private void AppBarButton_Click_SelectImagePath(object sender, RoutedEventArgs e) { }//m_Control.SelectImage(Img_SelectTexture, x_TextBlock_TexturePath); }

        private void AppBarButton_Click_SelectFilePath(object sender, RoutedEventArgs e) { }//m_Control.SelectFile(x_TextBlock_FilePath); }

        private void FirePropertyChanged([CallerMemberName] string memberName = null) { _EventPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName)); }
    }
}
