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
using System.Linq;

namespace Editor.Controls
{
    internal struct Category
    {
        public string Name;
        public Symbol Symbol;
        public string Glyph;
        public bool DefaultColor;
        public string[] FileTypes;
        public bool PreviewTile;
    }

    internal class FilesController
    {
        public string RootPath { get; private set; }
        public string CurrentProjectTitle { get; private set; }

        public WrapPanel Wrap;
        public Category[] Categories;

        private Category? _currentCategory;
        private string _currentSubPath;

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

            foreach (StorageFile file in files)
            {
                foreach (var info in Categories)
                    foreach (var type in info.FileTypes)
                        if (type == file.FileType)
                        {
                            string destCategoryPath = Path.Combine(RootPath, info.Name);
                            string destFilePath = Path.Combine(destCategoryPath, file.Name);

                            File.Copy(file.Path, destFilePath, true);
                        }
            }

            Refresh();
        }

        public void OpenFolder()
        {
            Process.Start(new ProcessStartInfo { FileName = RootPath, UseShellExecute = true });
        }

        public void Refresh()
        {
            ValidateCategoriesExist();

            ValidateCorrectFileTypesAsync();

            if (_currentCategory is null)
                CreateCatergoryTiles(Categories);
            else
                CreateFileSystemEntryTilesAsync();

        }

        public void ValidateCategoriesExist()
        {
            string categoryPath;

            foreach (var info in Categories)
                if (!Directory.Exists(categoryPath = Path.Combine(RootPath, info.Name)))
                    Directory.CreateDirectory(categoryPath);
        }

        public async void ValidateCorrectFileTypesAsync()
        {
            bool dirty = false;

            foreach (var info in Categories)
            {
                var filePaths = Directory.EnumerateFiles(Path.Combine(RootPath, info.Name));

                foreach (var path in filePaths)
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);

                    if (!info.FileTypes.Contains(file.FileType))
                        foreach (var info2 in Categories)
                            foreach (var fileTypes2 in info2.FileTypes)
                                if (file.FileType == fileTypes2)
                                {
                                    File.Move(
                                        file.Path,
                                        Path.Combine(RootPath, Path.Combine(info2.Name, file.Name)),
                                        true);

                                    if (_currentCategory != null)
                                        if (info.Equals(_currentCategory.Value) || info2.Equals(_currentCategory.Value))
                                            dirty = true;
                                }
                }
            }

            if (dirty)
                CreateFileSystemEntryTilesAsync();
        }

        public void CreateCatergoryTiles(params Category[] categories)
        {
            Categories = categories;

            Wrap.Children.Clear();

            Wrap.VerticalSpacing = 10;

            foreach (var info in Categories)
            {
                Grid icon;

                if (string.IsNullOrEmpty(info.Glyph))
                    icon = CreateIcon(info.Symbol, !info.DefaultColor);
                else
                    icon = CreateIcon(info.Glyph, !info.DefaultColor);

                Wrap.Children.Add(CategoryTile(info, icon, !info.DefaultColor));
            }
        }

        public async void CreateFileSystemEntryTilesAsync()
        {
            Wrap.Children.Clear();

            Wrap.VerticalSpacing = 35;

            Wrap.Children.Add(BackTile(CreateIcon(Symbol.Back, false)));

            string currentPath = Path.Combine(RootPath, _currentCategory.Value.Name);

            if (!string.IsNullOrEmpty(_currentSubPath))
                currentPath = Path.Combine(currentPath, _currentSubPath);

            var folderPaths = Directory.EnumerateDirectories(currentPath);

            foreach (var path in folderPaths)
            {
                Grid icon = CreateIcon(Symbol.Folder);

                Wrap.Children.Add(FolderTile(path, icon));
            }

            var filePaths = Directory.EnumerateFiles(currentPath);

            foreach (var path in filePaths)
            {
                Grid icon = CreateIcon();

                Image image = new Image() { Width = 145, Height = 90 };

                if (_currentCategory.Value.PreviewTile)
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);

                    _ = PreviewFileToImageAsync(file, image);
                }

                Wrap.Children.Add(FileTile(path, icon, image));
            }
        }

        private Grid CategoryTile(Category category, Grid icon, bool rndColor = true)
        {
            Grid grid = new Grid();

            Button button = new Button()
            {
                Width = 145,
                Height = 90,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                DataContext = category,
            };

            button.Click += (s, e) =>
            {
                _currentCategory = (Category)((Button)e.OriginalSource).DataContext;

                Refresh();
            };

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

            TextBlock label = new TextBlock() { Text = category.Name };

            if (rndColor)
                label.Foreground = new SolidColorBrush(Colors.White);

            viewbox.Child = icon;
            stack.Children.Add(viewbox);
            stack.Children.Add(label);
            button.Content = stack;
            grid.Children.Add(button);

            return grid;
        }

        private Grid FileTile(string path, Grid icon, Image image)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, -30) };

            Grid grid2 = new Grid();

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            StackPanel stack = new StackPanel() { Spacing = 5 };

            StackPanel stack2 = new StackPanel() { Spacing = 5, VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = HorizontalAlignment.Center };

            Button button = new Button()
            {
                Width = 145,
                Height = 90,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            button.DoubleTapped += (s, e) =>
            {
                if (File.Exists(path))
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            };

            TextBlock fileType = new TextBlock() { Text = Path.GetExtension(path) };

            TextBlock label = new TextBlock()
            {
                Text = Path.GetFileNameWithoutExtension(path),
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 140,
            };

            viewbox.Child = icon;
            stack2.Children.Add(viewbox);
            stack2.Children.Add(fileType);
            grid2.Children.Add(stack2);
            grid2.Children.Add(image);
            button.Content = grid2;
            stack.Children.Add(button);
            stack.Children.Add(label);
            grid.Children.Add(stack);

            return grid;
        }

        private Grid FolderTile(string path, Grid icon)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, -30) };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            StackPanel stack = new StackPanel() { Spacing = 5 };

            Button button = new Button()
            {
                Width = 145,
                Height = 90,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            button.Background = new SolidColorBrush(new Color()
            {
                A = 255,
                R = (byte)new Random().Next(64, 128),
                B = (byte)new Random().Next(64, 128),
                G = (byte)new Random().Next(64, 128)
            });

            button.Click += (s, e) =>
            {
                _currentSubPath = Path.GetRelativePath(
                    Path.Combine(RootPath, _currentCategory.Value.Name),
                    path);

                Refresh();
            };

            TextBlock label = new TextBlock()
            {
                Text = Path.GetFileName(path),
                HorizontalAlignment = HorizontalAlignment.Center,
                MaxWidth = 140,
            };

            viewbox.Child = icon;
            button.Content = viewbox;
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

            button.Click += (s, e) =>
            {
                if (!string.IsNullOrEmpty(_currentSubPath))
                {
                    if (!_currentSubPath.Contains('\\'))
                        _currentSubPath = null;
                    else
                    {
                        var pathArr = _currentSubPath.Split('\\').SkipLast(1);

                        _currentSubPath = string.Join('\\', pathArr);
                    }

                    Refresh();
                }
                else
                {
                    _currentCategory = null;
                    _currentSubPath = null;

                    CreateCatergoryTiles(Categories);
                }
            };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            viewbox.Child = icon;
            button.Content = viewbox;
            grid.Children.Add(button);

            return grid;
        }

        private Grid CreateIcon()
        {
            Grid grid = new Grid();
            dynamic icon;

            if (string.IsNullOrEmpty(_currentCategory.Value.Glyph))
                icon = new SymbolIcon() { Symbol = _currentCategory.Value.Symbol };
            else
                icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = _currentCategory.Value.Glyph };

            grid.Children.Add(icon);

            return grid;
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
