using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Editor.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Properties : UserControl
    {
        internal event PropertyChangedEventHandler EventPropertyChanged;

        internal PropertiesController properitesControl;


        public Properties()
        {
            this.InitializeComponent();

            properitesControl = new PropertiesController();
            List<Grid> collection = new List<Grid>();
            collection.Add(properitesControl.CreateColorButton());
            collection.Add(properitesControl.CreateNumberInput());
            collection.Add(properitesControl.CreateTextInput());
            collection.Add(properitesControl.CreateVec2Input());
            collection.Add(properitesControl.CreateVec3Input());
            collection.Add(properitesControl.CreateSlider());
            collection.Add(properitesControl.CreateBool());
            collection.Add(properitesControl.CreateTextureSlot());
            collection.Add(properitesControl.CreateReferenceSlot());
            collection.Add(properitesControl.CreateHeader());
            collection.Add(properitesControl.WrapExpander(properitesControl.CreateEvent()));
            x_StackPanel_Properties.Children.Add(properitesControl.CreateScript("Example", collection.ToArray()));
            x_StackPanel_Properties.Children.Add(properitesControl.CreateScript("Another", properitesControl.CreateSpacer()));
        }


        void AppBarButton_Click_SelectImagePath(object sender, RoutedEventArgs e) { }//m_Control.SelectImage(Img_SelectTexture, x_TextBlock_TexturePath); }
        void AppBarButton_Click_SelectFilePath(object sender, RoutedEventArgs e) { }//m_Control.SelectFile(x_TextBlock_FilePath); }
        void FirePropertyChanged([CallerMemberName] string memberName = null) { this.EventPropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName)); }
    }
}
