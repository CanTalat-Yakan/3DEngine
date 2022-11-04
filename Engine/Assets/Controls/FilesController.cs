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
            var picker = new Windows.Storage.Pickers.FileOpenPicker()
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

            foreach (StorageFile file in files)
            {
                foreach (var info in Categories)
                    foreach (var type in info.SupportedFileTypes)
                        if (type == file.FileType)
                        {
                            ValidateCategoriesExist();

                            string subFolderPath = Path.Combine(RootPath, info.Name);
                            string destFile = Path.Combine(subFolderPath, file.Name);

                            // To copy a file to another location and
                            // overwrite the destination file if it already exists.
                            System.IO.File.Copy(file.Path, destFile, true);
                        }
            }
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
            Wrap.Children.Clear();

            Categories = categories;

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
            Wrap.Children.Clear();

            var butoon = (Button)e.OriginalSource;
            var category = (string)butoon.DataContext;

            var filePaths = Directory.EnumerateFiles(Path.Combine(RootPath, category));

            Grid icon = CreateIcon(Symbol.Back, false);
            Wrap.Children.Add(BackTile("Back", icon));
            foreach (var path in filePaths)
            {
                var file = await StorageFile.GetFileFromPathAsync(path);
                Wrap.Children.Add(FileTile(file, new Grid()));
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
                Width = 150,
                Height = 100,
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
                    R = (byte)new Random().Next(64, 192),
                    B = (byte)new Random().Next(64, 192),
                    G = (byte)new Random().Next(64, 192)
                });

            StackPanel stack = new StackPanel() { Spacing = 20 };

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
            Grid grid = new Grid();

            Image image = new Image();

            //using (IRandomAccessStream fileStream = await file.OpenAsync(Windows.Storage.FileAccessMode.Read))
            //{
            //    BitmapImage bitmapImage = new BitmapImage() { DecodePixelHeight = 48, DecodePixelWidth = 48 };
            //    await bitmapImage.SetSourceAsync(fileStream);
            //    image.Source = bitmapImage;
            //}

            StackPanel stack = new StackPanel() { Spacing = 5 };

            Button button = new Button()
            {
                Width = 150,
                Height = 100,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                DataContext = file.Name,
            };

            button.Click += (s, e) =>
            {
                if (File.Exists(file.Path))
                    new Process
                    {
                        StartInfo = new ProcessStartInfo(file.Path) { UseShellExecute = true }
                    }.Start();
            };

            TextBlock label = new TextBlock()
            {
                Text = file.Name,
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 150,
                Margin = new Thickness(0, 0, 0, -30)
            };

            button.Content = icon;
            stack.Children.Add(button);
            stack.Children.Add(label);
            grid.Children.Add(image);
            grid.Children.Add(stack);

            return grid;
        }

        private Grid BackTile(string s, Grid icon)
        {
            Grid grid = new Grid();

            Button button = new Button()
            {
                Width = 150,
                Height = 100,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                DataContext = s,
            };

            button.Click += (s, e) => { CreateCatergoryTiles(Categories); };

            StackPanel stack = new StackPanel() { Spacing = 20 };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            TextBlock label = new TextBlock() { Text = s };

            viewbox.Child = icon;

            stack.Children.Add(viewbox);
            stack.Children.Add(label);

            button.Content = stack;
            grid.Children.Add(button);

            return grid;
        }
    }
}
