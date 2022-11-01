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
        internal event PropertyChangedEventHandler m_PropertyChanged;

        internal CProperties m_Control;


        public Properties()
        {
            this.InitializeComponent();

            m_Control = new CProperties();
            List<Grid> collection = new List<Grid>();
            collection.Add(m_Control.CreateColorButton());
            collection.Add(m_Control.CreateNumberInput());
            collection.Add(m_Control.CreateTextInput());
            collection.Add(m_Control.CreateVec2Input());
            collection.Add(m_Control.CreateVec3Input());
            collection.Add(m_Control.CreateSlider());
            collection.Add(m_Control.CreateBool());
            collection.Add(m_Control.CreateTextureSlot());
            collection.Add(m_Control.CreateReferenceSlot());
            collection.Add(m_Control.CreateHeader());
            collection.Add(m_Control.WrapExpander(m_Control.CreateEvent()));
            x_StackPanel_Properties.Children.Add(m_Control.CreateScript("Example", collection.ToArray()));
            x_StackPanel_Properties.Children.Add(m_Control.CreateScript("Another", m_Control.CreateSpacer()));
        }


        void AppBarButton_Click_SelectImagePath(object sender, RoutedEventArgs e) { }//m_Control.SelectImage(Img_SelectTexture, x_TextBlock_TexturePath); }
        void AppBarButton_Click_SelectFilePath(object sender, RoutedEventArgs e) { }//m_Control.SelectFile(x_TextBlock_FilePath); }
        void FirePropertyChanged([CallerMemberName] string memberName = null) { this.m_PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(memberName)); }
    }
}
