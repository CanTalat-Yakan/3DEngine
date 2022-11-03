using Editor.Controls;
using Microsoft.UI.Xaml.Controls;
using System.Collections.Generic;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Files : UserControl
    {
        public Grid ChangeColorWithTheme;

        internal FilesController _filesControl = new FilesController();

        public Files()
        {
            this.InitializeComponent();

            ChangeColorWithTheme = x_Grid_ChangeColorWithTheme;

            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Scenes", "\xEA86"));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Scripts", Symbol.Document));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Prefabs", "\xE734"));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Models", "\xF158"));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Animation", "\xE805"));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Materials", "\xF156"));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Textures", "\xEB9F"));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Audio", Symbol.Audio));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Videos", Symbol.Video));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Fonts", Symbol.Font));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Documents", "\xEA90"));
            x_WrapPanel_Files.Children.Add(_filesControl.CategoryTile("Packages", "\xE74C", false));
        }
    }
}
