using Editor.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Storage;
using Windows.Storage.Streams;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.UserControls
{
    public sealed partial class Files : UserControl
    {
        public Grid ChangeColorWithTheme;

        internal FilesController _filesControl;

        public Files()
        {
            this.InitializeComponent();

            ChangeColorWithTheme = x_Grid_ChangeColorWithTheme;

            _filesControl = new FilesController(x_WrapPanel_Files);

            _filesControl.CreateCatergoryTiles(
                new Category() { Name = "Scene", Glyph = "\xEA86", SupportedFileTypes = new string[] { ".usd", ".usda", ".usdc", ".usdz" } },
                new Category() { Name = "Scripts", Symbol = Symbol.Document, SupportedFileTypes = new string[] { ".cs" } },
                new Category() { Name = "Prefabs", Glyph = "\xE734", SupportedFileTypes = new string[] { ".prefab" } },
                new Category() { Name = "Models", Glyph = "\xF158", SupportedFileTypes = new string[] { ".fbx", ".obj", ".blend", ".3ds", ".dae" } },
                new Category() { Name = "Animation", Glyph = "\xE805", SupportedFileTypes = new string[] { ".fbx", ".dae" } },
                new Category() { Name = "Materials", Glyph = "\xF156", SupportedFileTypes = new string[] { ".material" } },
                new Category() { Name = "Textures", Glyph = "\xEB9F", SupportedFileTypes = new string[] { ".png", ".jpg", ".jpeg", ".tiff", ".tga", ".psd", ".bmp", } },
                new Category() { Name = "Audio", Symbol = Symbol.Audio, SupportedFileTypes = new string[] { ".m4a", ".mp3", ".wav", ".ogg" } },
                new Category() { Name = "Videos", Symbol = Symbol.Video, SupportedFileTypes = new string[] { ".m4v", ".mp4", ".mov", ".avi" } },
                new Category() { Name = "Fonts", Symbol = Symbol.Font, SupportedFileTypes = new string[] { ".ttf", ".otf" } },
                new Category() { Name = "Documents", Glyph = "\xEA90", SupportedFileTypes = new string[] { ".pdf", ".txt", ".doc", ".docx" } },
                new Category() { Name = "Packages", Glyph = "\xE74C", DefaultColor = true, SupportedFileTypes = new string[] { ".zip", ".7zip", ".winrar" } });

            _filesControl.ValidateCategoriesExist();
        }

        private void AppBarButton_Click_AddFiles(object sender, RoutedEventArgs e) { _filesControl.SelectFilesAsync(); }

        private void AppBarButton_Click_RefreshFiles(object sender, RoutedEventArgs e) { _filesControl.Refresh(); }
    }
}
