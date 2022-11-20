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
using Editor.UserControls;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Editor.Controls
{
    internal struct Category
    {
        public string Name;
        public Symbol Symbol;
        public string Glyph;
        public string[] FileTypes;
        public bool Thumbnail;
        public bool Creatable;
    }

    internal class FilesController
    {
        public string RootPath { get; private set; }
        public string CurrentProjectTitle { get; private set; }

        public WrapPanel Wrap;

        public BreadcrumbBar Bar;

        public Category[] Categories;

        private Files _files;

        private Category? _currentCategory;
        private string _currentSubPath;

        private static readonly string TEMPLATES = @"Assets\Engine\Resources\Templates";

        public FilesController(Files files, WrapPanel wrap, BreadcrumbBar bar)
        {
            Wrap = wrap;
            Bar = bar;

            _files = files;

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
            };

            if (_currentCategory != null)
                foreach (var type in _currentCategory.Value.FileTypes)
                    picker.FileTypeFilter.Add(type);
            else
                picker.FileTypeFilter.Add("*");

            // Make sure to get the HWND from a Window object,
            // pass a Window reference to GetWindowHandle.
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.Window as MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            var files = await picker.PickMultipleFilesAsync();

            foreach (StorageFile file in files)
            {
                if (file != null)
                    foreach (var category in Categories)
                        foreach (var type in category.FileTypes)
                            if (type == file.FileType)
                            {
                                string destCategoryPath = Path.Combine(RootPath, category.Name);
                                string destFilePath = Path.Combine(destCategoryPath, file.Name);

                                File.Copy(file.Path, destFilePath, true);
                            }
            }

            Refresh();
        }

        public void OpenFolder()
        {
            var path = RootPath;

            if (_currentCategory != null)
            {
                path = Path.Combine(RootPath, _currentCategory.Value.Name);

                if (!string.IsNullOrEmpty(_currentSubPath))
                    path = Path.Combine(path, _currentSubPath);
            }

            if (Directory.Exists(path))
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }

        public void Refresh()
        {
            ValidateCategoriesExist();
            ValidateCorrectFileTypesAsync();

            if (_currentCategory is null)
                CreateCatergoryTiles(Categories);
            else
                CreateFileSystemEntryTilesAsync();

            SetBreadcrumbBar();
        }

        public void ValidateCategoriesExist()
        {
            string categoryPath;

            foreach (var category in Categories)
                if (!Directory.Exists(categoryPath = Path.Combine(RootPath, category.Name)))
                    Directory.CreateDirectory(categoryPath);
        }

        public async void ValidateCorrectFileTypesAsync()
        {
            bool dirty = false;

            foreach (var category in Categories)
            {
                var filePaths = Directory.EnumerateFiles(Path.Combine(RootPath, category.Name));

                foreach (var path in filePaths)
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);

                    if (!category.FileTypes.Contains(file.FileType))
                        foreach (var category2 in Categories)
                            foreach (var fileTypes2 in category2.FileTypes)
                                if (file.FileType == fileTypes2)
                                {
                                    File.Move(
                                        file.Path,
                                        IncrementFileIfExists(Path.Combine(RootPath, category2.Name, file.Name)));

                                    if (_currentCategory != null)
                                        if (category.Equals(_currentCategory.Value) || category2.Equals(_currentCategory.Value))
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

            foreach (var category in Categories)
            {
                Grid icon;

                if (string.IsNullOrEmpty(category.Glyph))
                    icon = CreateIcon(category.Symbol);
                else
                    icon = CreateIcon(category.Glyph);

                Wrap.Children.Add(CategoryTile(category, icon));
            }

            SetBreadcrumbBar();
        }

        public async void CreateFileSystemEntryTilesAsync()
        {
            Wrap.Children.Clear();

            Wrap.VerticalSpacing = 35;

            Wrap.Children.Add(BackTile(CreateIcon(Symbol.Back)));

            Wrap.Children.Add(AddTile(CreateIcon(Symbol.Add)));

            string currentPath = Path.Combine(RootPath, _currentCategory.Value.Name);

            if (!string.IsNullOrEmpty(_currentSubPath))
            {
                currentPath = Path.Combine(currentPath, _currentSubPath);

                // When a directoy is deleted and you refresh
                // Go up a directoy and if it exists continue, if not just display category folder
                while (!Directory.Exists(currentPath))
                {
                    _currentSubPath = GoUpDirectory(_currentSubPath);

                    if (string.IsNullOrEmpty(_currentSubPath))
                    {
                        currentPath = Path.Combine(RootPath, _currentCategory.Value.Name);
                        break;
                    }

                    currentPath = Path.Combine(RootPath, _currentCategory.Value.Name, _currentSubPath);
                }
            }

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

                if (_currentCategory.Value.Thumbnail)
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);

                    _ = PreviewFileToImageAsync(file, image);
                }

                Wrap.Children.Add(FileTile(path, icon, image));
            }
        }

        private Grid CategoryTile(Category category, Grid icon)
        {
            Grid grid = new Grid() { Padding = new Thickness(-1), CornerRadius = new CornerRadius(10) };

            grid.Background = new SolidColorBrush(new Color()
            {
                A = 255,
                R = (byte)new Random().Next(32, 96),
                B = (byte)new Random().Next(32, 96),
                G = (byte)new Random().Next(32, 96)
            });

            Button button = new Button()
            {
                Width = 145,
                Height = 90,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                DataContext = category,
            };

            button.Click += (s, e) =>
            {
                _currentCategory = (Category)((Button)e.OriginalSource).DataContext;

                Refresh();
            };

            StackPanel stack = new StackPanel() { Spacing = 5 };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            TextBlock label = new TextBlock() { Text = category.Name };

            viewbox.Child = icon;
            stack.Children.Add(viewbox);
            stack.Children.Add(label);
            button.Content = stack;
            grid.Children.Add(button);

            return grid;
        }

        private Grid FileTile(string path, Grid icon, Image image)
        {
            image.Opacity = 0.5f;

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

            button.Tapped += (s, e) =>
            {
                PropertiesController.Clear();
                PropertiesController.Set(new Properties(path));
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
            grid2.Children.Add(image);
            grid2.Children.Add(stack2);
            button.Content = grid2;
            stack.Children.Add(button);
            stack.Children.Add(label);
            grid.Children.Add(stack);

            return grid;
        }

        private Grid FolderTile(string path, Grid icon)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, -30) };

            Grid grid2 = new Grid() { Padding = new Thickness(-1), CornerRadius = new CornerRadius(10) };

            grid2.Background = new SolidColorBrush(new Color()
            {
                A = 255,
                R = (byte)new Random().Next(32, 96),
                B = (byte)new Random().Next(32, 96),
                G = (byte)new Random().Next(32, 96)
            });

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            StackPanel stack = new StackPanel() { Spacing = 5 };

            Button button = new Button()
            {
                Width = 145,
                Height = 90,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };


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
            grid2.Children.Add(button);
            stack.Children.Add(grid2);
            stack.Children.Add(label);
            grid.Children.Add(stack);

            return grid;
        }

        private Grid BackTile(Grid icon)
        {
            Grid grid = new Grid();

            Button button = new Button()
            {
                Width = 67,
                Height = 90,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            button.Click += (s, e) =>
            {
                GoUpDirectoryAndRefresh();
            };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            viewbox.Child = icon;
            button.Content = viewbox;
            grid.Children.Add(button);

            return grid;
        }

        private Grid AddTile(Grid icon)
        {
            Grid grid = new Grid();

            Button button = new Button()
            {
                Width = 68,
                Height = 90,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };

            button.Click += (s, e) =>
            {
                if (_currentCategory.Value.Creatable)
                    ContentDialogCreateNewFileOrFolderAsync();
                else
                    ContentDialogCreateNewFolderAsync();

                Refresh();
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

        private Grid CreateIcon(string glyph)
        {
            Grid grid = new Grid();

            FontIcon icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = glyph };

            grid.Children.Add(icon);

            return grid;
        }

        private Grid CreateIcon(Symbol symbol)
        {
            Grid grid = new Grid();

            SymbolIcon symbolIcon = new SymbolIcon() { Symbol = symbol };

            grid.Children.Add(symbolIcon);

            return grid;
        }

        private string GoUpDirectory(string path)
        {
            if (!path.Contains('\\'))
                path = null;
            else
            {
                var pathArr = path.Split('\\').SkipLast(1);

                path = string.Join('\\', pathArr);
            }
            return path;
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

        private async void CreateDialogAsync(ContentDialog contentDialog)
        {
            var result = await contentDialog.ShowAsync();
        }

        private async void ContentDialogCreateNewFileOrFolderAsync()
        {
            var dialog = new ContentDialog()
            {
                XamlRoot = _files.XamlRoot,
                Title = "Create a new file system entry",
                PrimaryButtonText = "File",
                SecondaryButtonText = "Folder",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
                ContentDialogCreateNewFileAsync();
            else if (result == ContentDialogResult.Secondary)
                ContentDialogCreateNewFolderAsync();

        }

        private async void ContentDialogCreateNewFileAsync()
        {
            TextBox fileName;

            var dialog = new ContentDialog()
            {
                XamlRoot = _files.XamlRoot,
                Title = "Create a new " + RemoveLastChar(_currentCategory.Value.Name),
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = fileName = new TextBox() { PlaceholderText = "New " + RemoveLastChar(_currentCategory.Value.Name) },
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // \w is equivalent of [0 - 9a - zA - Z_]."
                if (!string.IsNullOrEmpty(fileName.Text))
                    if (!Regex.Match(fileName.Text, @"^[\w\-.]+$").Success)
                    {
                        CreateDialogAsync(new ContentDialog()
                        {
                            XamlRoot = _files.XamlRoot,
                            Title = "A file can't contain any of the following characters",
                            CloseButtonText = "Close",
                            DefaultButton = ContentDialogButton.Close,
                            Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                        });

                        return;
                    }

                string path = Path.Combine(RootPath, _currentCategory.Value.Name);

                if (_currentSubPath != null)
                    path = Path.Combine(RootPath, _currentCategory.Value.Name, _currentSubPath);

                if (string.IsNullOrEmpty(fileName.Text))
                    path = Path.Combine(path, "New " + RemoveLastChar(_currentCategory.Value.Name) + _currentCategory.Value.FileTypes[0]);
                else if (char.IsDigit(fileName.Text[0]))
                    path = Path.Combine(path, "_" + fileName.Text + _currentCategory.Value.FileTypes[0]);
                else
                    path = Path.Combine(path, fileName.Text + _currentCategory.Value.FileTypes[0]);

                path = IncrementFileIfExists(path);

                WriteFileFromTemplatesAsync(path);

                CreateFileSystemEntryTilesAsync();

                PropertiesController.Clear();
                PropertiesController.Set(new Properties(path));
            }
        }

        private async void ContentDialogCreateNewFolderAsync()
        {
            TextBox fileName;

            var dialog = new ContentDialog()
            {
                XamlRoot = _files.XamlRoot,
                Title = "Create a new folder",
                PrimaryButtonText = "Save",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = fileName = new TextBox() { PlaceholderText = "New folder" },
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                // \w is equivalent of [0 - 9a - zA - Z_]."
                if (!string.IsNullOrEmpty(fileName.Text))
                    if (!Regex.Match(fileName.Text, @"^[\w\-.]+$").Success)
                    {
                        CreateDialogAsync(new ContentDialog()
                        {
                            XamlRoot = _files.XamlRoot,
                            Title = "A folder can't contain any of the following characters",
                            CloseButtonText = "Close",
                            DefaultButton = ContentDialogButton.Close,
                            Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                        });

                        return;
                    }

                string path = Path.Combine(RootPath, _currentCategory.Value.Name);

                if (_currentSubPath != null)
                    path = Path.Combine(RootPath, _currentCategory.Value.Name, _currentSubPath);

                if (string.IsNullOrEmpty(fileName.Text))
                    path = Path.Combine(path, "New folder");
                else if (char.IsDigit(fileName.Text[0]))
                    path = Path.Combine(path, "_" + fileName.Text);
                else
                    path = Path.Combine(path, fileName.Text);

                path = IncrementFolderIfExists(path);

                Directory.CreateDirectory(path);

                CreateFileSystemEntryTilesAsync();
            }
        }

        private async void WriteFileFromTemplatesAsync(string path)
        {
            string templatePath = Path.Combine(AppContext.BaseDirectory, TEMPLATES, _currentCategory.Value.Name + ".txt");

            using (FileStream fs = File.Create(path))
                if (File.Exists(templatePath))
                {
                    // writing data in string
                    string[] lines = await File.ReadAllLinesAsync(templatePath);
                    string joinedLines = string.Join("\n", lines);

                    string name = Path.GetFileNameWithoutExtension(path);

                    if (joinedLines.Contains("{{FileName}}"))
                        joinedLines = joinedLines.Replace("{{FileName}}", Regex.Replace(name, @"[\s+\(\)]", ""));

                    byte[] info = new UTF8Encoding(true).GetBytes(joinedLines);

                    fs.Write(info, 0, info.Length);
                }
        }

        private string RemoveLastChar(string s) { return s.Remove(s.Length - 1); }

        private string IncrementFileIfExists(string path)
        {
            var fileCount = 0;

            while (File.Exists(
                Path.Combine(
                    GoUpDirectory(path),
                    Path.GetFileNameWithoutExtension(path) +
                        (fileCount > 0
                        ? " (" + (fileCount + 1).ToString() + ")"
                        : "")
                    + Path.GetExtension(path))))
                fileCount++;

            return Path.Combine(
                GoUpDirectory(path),
                Path.GetFileNameWithoutExtension(path) +
                    (fileCount > 0
                    ? " (" + (fileCount + 1).ToString() + ")"
                    : "")
                + Path.GetExtension(path));
        }

        private string IncrementFolderIfExists(string path)
        {
            var fileCount = 0;

            while (Directory.Exists(
                Path.Combine(
                    GoUpDirectory(path),
                    Path.GetFileNameWithoutExtension(path) +
                        (fileCount > 0
                        ? " (" + (fileCount + 1).ToString() + ")"
                        : ""))))
                fileCount++;

            return Path.Combine(
                GoUpDirectory(path),
                Path.GetFileNameWithoutExtension(path) +
                    (fileCount > 0
                    ? " (" + (fileCount + 1).ToString() + ")"
                    : ""));
        }

        private void SetBreadcrumbBar()
        {
            if (_currentCategory is null)
                Bar.ItemsSource = new string[] { };
            else
            {
                var source = new string[] { _currentCategory.Value.Name };

                Bar.ItemsSource = source;

                if (!string.IsNullOrEmpty(_currentSubPath))
                {
                    var subPathSource = _currentSubPath.Split('\\');

                    var newSource = new string[source.Length + subPathSource.Length];

                    source.CopyTo(newSource, 0);
                    subPathSource.CopyTo(newSource, source.Length);

                    Bar.ItemsSource = newSource;
                }
            }
        }

        public void GoUpDirectoryAndRefresh()
        {
            if (!string.IsNullOrEmpty(_currentSubPath))
            {
                _currentSubPath = GoUpDirectory(_currentSubPath);

                Refresh();
            }
            else
            {
                _currentCategory = null;
                _currentSubPath = null;

                CreateCatergoryTiles(Categories);
            }
        }
    }
}