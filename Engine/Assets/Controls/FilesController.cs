using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using System;
using Microsoft.UI;
using System.IO;
using CommunityToolkit.WinUI.UI.Controls;
using Windows.Storage;
using Windows.Storage.Pickers;
using System.Diagnostics;
using Windows.Storage.Streams;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Threading.Tasks;

namespace Editor.Controls
{
    internal struct Category
    {
        public string Name;
        public Symbol Symbol;
        public string Glyph;
        public bool DefaultColor;
        public string[] SupportedFileTypes;
    }

    internal class FilesController
    {
        public string RootPath { get; private set; }
        public string CurrentProjectTitle { get; private set; }

        public WrapPanel Wrap;
        public Category[] Categories;

        public FilesController(WrapPanel wrap)
        {
            Wrap = wrap;

            var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

            RootPath = Path.Combine(documentsDir, "3DEngine");
            CurrentProjectTitle = "New Project";

            if (!string.IsNullOrEmpty(CurrentProjectTitle))
                RootPath = Path.Combine(RootPath, CurrentProjectTitle);
        }

        public async void SelectFilesAsync()
        {
            ValidateCategoriesExist();

            var picker = new FileOpenPicker()
            {
                ViewMode = PickerViewMode.Thumbnail,
                SuggestedStartLocation = PickerLocationId.Desktop,
                FileTypeFilter = { "*" }
            };

            // Make sure to get the HWND from a Window object,
            // pass a Window reference to GetWindowHandle.
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.Window as MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();

            ValidateCategoriesExist();

            foreach (StorageFile file in files)
            {
                foreach (var info in Categories)
                    foreach (var type in info.SupportedFileTypes)
                        if (type == file.FileType)
                        {
                            string subFolderPath = Path.Combine(RootPath, info.Name);
                            string destFile = Path.Combine(subFolderPath, file.Name);

                            // To copy a file to another location and
                            // overwrite the destination file if it already exists.
                            System.IO.File.Copy(file.Path, destFile, true);
                        }
            }
        }

        public void OpenFolder()
        {
            Process.Start(new ProcessStartInfo { FileName = RootPath, UseShellExecute = true });
        }

        public void Refresh()
        {
            ValidateCategoriesExist();
        }

        public void ValidateCategoriesExist()
        {
            string subFolderPath;

            foreach (var info in Categories)
                if (!Directory.Exists(subFolderPath = Path.Combine(RootPath, info.Name)))
                    Directory.CreateDirectory(subFolderPath);
        }

        public void CreateCatergoryTiles(params Category[] categories)
        {
            Categories = categories;

            ValidateCategoriesExist();

            Wrap.Children.Clear();

            foreach (var info in Categories)
            {
                Grid icon;

                if (string.IsNullOrEmpty(info.Glyph))
                    icon = CreateIcon(info.Symbol, !info.DefaultColor);
                else
                    icon = CreateIcon(info.Glyph, !info.DefaultColor);

                Wrap.Children.Add(CategoryTile(info.Name, icon, !info.DefaultColor));
            }
        }

        public async void CreateFileTiles(RoutedEventArgs e)
        {
            ValidateCategoriesExist();

            Wrap.Children.Clear();

            var butoon = (Button)e.OriginalSource;
            var category = (string)butoon.DataContext;

            var filePaths = Directory.EnumerateFiles(Path.Combine(RootPath, category));

            Grid icon = CreateIcon(Symbol.Back, false);
            Wrap.Children.Add(BackTile(icon));

            foreach (var info in Categories)
                if (info.Name == category)
                    if (string.IsNullOrEmpty(info.Glyph))
                        icon = CreateIcon(info.Symbol, !info.DefaultColor);
                    else
                        icon = CreateIcon(info.Glyph, !info.DefaultColor);

            foreach (var path in filePaths)
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                Wrap.Children.Add(FileTile(file, icon));
            }
        }

        private Grid CreateIcon(string glyph, bool rndColor = true)
        {
            Grid grid = new Grid();

            FontIcon icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = glyph };

            if (rndColor)
                icon.Foreground = new SolidColorBrush(Colors.White);

            grid.Children.Add(icon);

            return grid;
        }

        private Grid CreateIcon(Symbol symbol, bool rndColor = true)
        {
            Grid grid = new Grid();

            SymbolIcon symbolIcon = new SymbolIcon() { Symbol = symbol };

            if (rndColor)
                symbolIcon.Foreground = new SolidColorBrush(Colors.White);

            grid.Children.Add(symbolIcon);

            return grid;
        }

        private Grid CategoryTile(string s, Grid icon, bool rndColor = true)
        {
            Grid grid = new Grid();

            Button button = new Button()
            {
                Width = 145,
                Height = 90,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                DataContext = s,
            };

            button.Click += (s, e) => { CreateFileTiles(e); };

            if (rndColor)
                button.Background = new SolidColorBrush(new Color()
                {
                    A = 255,
                    R = (byte)new Random().Next(64, 128),
                    B = (byte)new Random().Next(64, 128),
                    G = (byte)new Random().Next(64, 128)
                });

            StackPanel stack = new StackPanel() { Spacing = 5 };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            TextBlock label = new TextBlock() { Text = s };

            if (rndColor)
                label.Foreground = new SolidColorBrush(Colors.White);

            viewbox.Child = icon;

            stack.Children.Add(viewbox);
            stack.Children.Add(label);

            button.Content = stack;
            grid.Children.Add(button);

            return grid;
        }

        private Grid FileTile(StorageFile file, Grid icon)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, -30) };

            Grid grid2 = new Grid();

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            Image image = new Image() { Width = 145, Height = 90 };

            _ = PreviewFileToImageAsync(file, image);

            StackPanel stack = new StackPanel() { Spacing = 5 };

            Button button = new Button()
            {
                Width = 145,
                Height = 90,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                DataContext = file.Name,
            };

            button.Click += (s, e) =>
            {
                if (File.Exists(file.Path))
                    Process.Start(new ProcessStartInfo { FileName = file.Path, UseShellExecute = true });
            };

            TextBlock label = new TextBlock()
            {
                Text = file.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 150,
            };

            viewbox.Child = icon;
            grid2.Children.Add(viewbox);
            grid2.Children.Add(image);
            button.Content = grid2;
            stack.Children.Add(button);
            stack.Children.Add(label);
            grid.Children.Add(stack);

            return grid;
        }

        private Grid BackTile(Grid icon)
        {
            Grid grid = new Grid();

            Button button = new Button()
            {
                Width = 145,
                Height = 90,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            button.Click += (s, e) => { CreateCatergoryTiles(Categories); };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            viewbox.Child = icon;
            button.Content = viewbox;
            grid.Children.Add(button);

            return grid;
        }

        private async Task PreviewFileToImageAsync(StorageFile file, Image image)
        {
            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapImage bitmapImage = new BitmapImage() { DecodePixelWidth = (int)image.Width, DecodePixelHeight = (int)image.Height };
                await bitmapImage.SetSourceAsync(fileStream);
                image.Source = bitmapImage;
            }
        }
    }
}
