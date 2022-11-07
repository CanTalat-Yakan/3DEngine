using Editor.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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

            _filesControl = new FilesController(this, x_WrapPanel_Files);
            
            _filesControl.CreateCatergoryTiles(
                new Category() { Name = "Scenes", Glyph = "\xEA86", FileTypes = new string[] { ".usd", ".usda", ".usdc", ".usdz" }, Creatable = true },
                new Category() { Name = "Scripts", Symbol = Symbol.Document, FileTypes = new string[] { ".cs" }, Creatable = true },
                new Category() { Name = "Prefabs", Glyph = "\xE734", FileTypes = new string[] { ".prefab" }, PreviewTile = true },
                new Category() { Name = "Models", Glyph = "\xF158", FileTypes = new string[] { ".fbx", ".obj", ".blend", ".3ds", ".dae" }, PreviewTile = true },
                new Category() { Name = "Animations", Glyph = "\xE805", FileTypes = new string[] { ".fbx", ".dae" } },
                new Category() { Name = "Materials", Glyph = "\xF156", FileTypes = new string[] { ".mat" }, PreviewTile = true, Creatable = true },
                new Category() { Name = "Textures", Glyph = "\xEB9F", FileTypes = new string[] { ".png", ".jpg", ".jpeg", ".tiff", ".tga", ".psd", ".bmp", }, PreviewTile = true },
                new Category() { Name = "Audios", Symbol = Symbol.Audio, FileTypes = new string[] { ".m4a", ".mp3", ".wav", ".ogg" } },
                new Category() { Name = "Videos", Symbol = Symbol.Video, FileTypes = new string[] { ".m4v", ".mp4", ".mov", ".avi" }, PreviewTile = true },
                new Category() { Name = "Fonts", Symbol = Symbol.Font, FileTypes = new string[] { ".ttf", ".otf" } },
                new Category() { Name = "Documents", Glyph = "\xF000", FileTypes = new string[] { ".txt", ".pdf", ".doc", ".docx" }, Creatable = true },
                new Category() { Name = "Packages", Glyph = "\xE74C", FileTypes = new string[] { ".zip", ".7zip", ".rar" }, DefaultColor = true });
        }

        private void AppBarButton_Click_AddFiles(object sender, RoutedEventArgs e) { _filesControl.SelectFilesAsync(); }

        private void AppBarButton_Click_OpenFolder(object sender, RoutedEventArgs e) { _filesControl.OpenFolder(); }

        private void AppBarButton_Click_RefreshFiles(object sender, RoutedEventArgs e) { _filesControl.Refresh(); }
    }
}
