using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.UI;
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Editor.UserControls;

namespace Editor.Controller
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

    internal partial class FilesController
    {
        public string RootPath { get; private set; }
        public string CurrentProjectTitle { get; private set; }

        public WrapPanel Wrap;

        public BreadcrumbBar Bar;

        public Grid GridMain;

        public Category[] Categories;

        private Files _files;

        private Category? _currentCategory;
        private string _currentSubPath;

        public FilesController(Files files, Grid grid, WrapPanel wrap, BreadcrumbBar bar)
        {
            GridMain = grid;
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
                                string targetPath = Path.Combine(RootPath, category.Name);
                                targetPath = Path.Combine(targetPath, file.Name);

                                File.Copy(file.Path, targetPath, true);
                            }
            }

            Refresh();
        }

        public void AddFileSystemEntry(StorageFile file)
        {
            if (file is null)
                return;

            if (string.IsNullOrEmpty(file.FileType))
                return;

            foreach (var category in Categories)
                foreach (var type in category.FileTypes)
                    if (type == file.FileType)
                    {
                        string targetPath = Path.Combine(RootPath, category.Name);
                        if (_currentCategory != null)
                            if (_currentCategory.Value.Name == category.Name)
                                if (!string.IsNullOrEmpty(_currentSubPath))
                                    targetPath = Path.Combine(targetPath, _currentSubPath);

                        PasteFile(file.Path, targetPath, DataPackageOperation.Copy);
                    }

            Refresh();
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

        private void GoIntoDirectoryAndRefresh(string path)
        {
            _currentSubPath = Path.GetRelativePath(
                Path.Combine(RootPath, _currentCategory.Value.Name),
                path);

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
            ValidateCorrectFileTypes();

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

        public void ValidateCorrectFileTypes()
        {
            bool dirty = false;

            if (_currentCategory is null)
                return;

            var targetPath = Path.Combine(RootPath, _currentCategory.Value.Name);
            if (!string.IsNullOrEmpty(_currentSubPath))
                targetPath = Path.Combine(targetPath, _currentSubPath);

            var filePaths = Directory.EnumerateFiles(targetPath);
            foreach (var path in filePaths)
                if (!_currentCategory.Value.FileTypes.Contains(Path.GetExtension(path)))
                    foreach (var category2 in Categories)
                        foreach (var fileTypes2 in category2.FileTypes)
                            if (Path.GetExtension(path) == fileTypes2)
                            {
                                File.Move(
                                    path,
                                    IncrementFileIfExists(Path.Combine(RootPath, category2.Name, Path.GetFileName(path))));

                                if (_currentCategory != null)
                                    if (_currentCategory.Value.Equals(_currentCategory.Value) || category2.Equals(_currentCategory.Value))
                                        dirty = true;
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
                Wrap.Children.Add(CategoryTile(category, CreateIcon(category)));

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
                Grid icon = CreateIcon(_currentCategory.Value);

                Image image = new Image() { Width = 145, Height = 90 };

                if (_currentCategory.Value.Thumbnail)
                {
                    var file = await StorageFile.GetFileFromPathAsync(path);

                    PreviewfileToImageAsync(file, image);
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
                Padding = new Thickness(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                DataContext = category,
            };
            button.Click += (s, e) =>
            {
                _currentCategory = (Category)((Button)e.OriginalSource).DataContext;

                Refresh();
            };

            Grid grid2 = new Grid() { HorizontalAlignment = HorizontalAlignment.Stretch };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            TextBlock label = new TextBlock() { Text = category.Name, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };

            viewbox.Child = icon;
            grid2.Children.Add(viewbox);
            grid2.Children.Add(label);
            button.Content = grid2;
            grid.Children.Add(button);

            #region // MenuFlyout
            GridMain.ContextFlyout = null;
            #endregion

            return grid;
        }

        private Grid FolderTile(string path, Grid icon)
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
                Height = 75,
                Padding = new Thickness(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            button.ContextFlyout = CreateDefaultMenuFlyout(path);
            button.Click += (s, e) => GoIntoDirectoryAndRefresh(path);

            Grid grid2 = new Grid() { HorizontalAlignment = HorizontalAlignment.Stretch };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            TextBlock label = new TextBlock()
            {
                Text = Path.GetFileName(path),
                FontSize = 12,
                MaxWidth = 140,
                HorizontalAlignment = HorizontalAlignment.Left,
                VerticalAlignment = VerticalAlignment.Bottom
            };

            viewbox.Child = icon;
            grid2.Children.Add(viewbox);
            grid2.Children.Add(label);
            button.Content = grid2;
            grid.Children.Add(button);

            return grid;
        }

        private Grid FileTile(string path, Grid icon, Image image)
        {
            image.Opacity = 0.5f;

            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, -30) };
            Grid grid2 = new Grid() { Padding = new Thickness(10) };
            Grid grid3 = new Grid();

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            TextBlock fileType = new TextBlock() { Text = Path.GetExtension(path), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };

            StackPanel stack = new StackPanel() { Spacing = 5 };

            Button button = new Button()
            {
                Width = 143,
                Height = 73,
                Padding = new Thickness(0),
                CornerRadius = new CornerRadius(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            button.ContextFlyout = CreateDefaultMenuFlyout(path, true);
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

            TextBlock label = new TextBlock()
            {
                Text = Path.GetFileNameWithoutExtension(path),
                FontSize = 12,
                MaxWidth = 140,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            viewbox.Child = icon;
            grid2.Children.Add(viewbox);
            grid2.Children.Add(fileType);
            grid3.Children.Add(image);
            grid3.Children.Add(grid2);
            button.Content = grid3;
            stack.Children.Add(button);
            stack.Children.Add(label);
            grid.Children.Add(stack);

            return grid;
        }

        private Grid BackTile(Grid icon)
        {
            var path = Path.Combine(RootPath, _currentCategory.Value.Name);
            if (!string.IsNullOrEmpty(_currentSubPath))
                path = Path.Combine(path, _currentSubPath);

            GridMain.ContextFlyout = CreateRootMenuFlyout(path);

            Grid grid = new Grid();

            Button button = new Button()
            {
                Width = 67,
                Height = 73,
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
                Width = 66,
                Height = 73,
                CornerRadius = new CornerRadius(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.Click += (s, e) =>
            {
                if (_currentCategory.Value.Creatable)
                    ContentDialogCreateNewFileOrFolderAndRefreshAsync();
                else
                    ContentDialogCreateNewFolderAndRefreshAsync();
            };

            Viewbox viewbox = new Viewbox() { MaxHeight = 24, MaxWidth = 24 };

            viewbox.Child = icon;
            button.Content = viewbox;
            grid.Children.Add(button);

            return grid;
        }
        private async void ContentDialogCreateNewFileOrFolderAndRefreshAsync(string path = "")
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
                ContentDialogCreateNewFileAsync(path);
            else if (result == ContentDialogResult.Secondary)
                ContentDialogCreateNewFolderAndRefreshAsync(path);
        }

        private async void ContentDialogCreateNewFileAsync(string path = "")
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

                var pathProvided = string.IsNullOrEmpty(path);
                if (pathProvided)
                {
                    path = Path.Combine(RootPath, _currentCategory.Value.Name);

                    if (_currentSubPath != null)
                        path = Path.Combine(RootPath, _currentCategory.Value.Name, _currentSubPath);
                }

                if (string.IsNullOrEmpty(fileName.Text))
                    path = Path.Combine(path, "New " + RemoveLastChar(_currentCategory.Value.Name) + _currentCategory.Value.FileTypes[0]);
                else if (char.IsDigit(fileName.Text[0]))
                    path = Path.Combine(path, "_" + fileName.Text + _currentCategory.Value.FileTypes[0]);
                else
                    path = Path.Combine(path, fileName.Text + _currentCategory.Value.FileTypes[0]);

                path = IncrementFileIfExists(path);

                await WriteFileFromTemplatesAsync(path);

                if (pathProvided)
                    CreateFileSystemEntryTilesAsync();

                Refresh();

                PropertiesController.Clear();
                PropertiesController.Set(new Properties(path));
            }
        }

        private async void ContentDialogCreateNewFolderAndRefreshAsync(string path = "")
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

                var pathProvided = string.IsNullOrEmpty(path);
                if (pathProvided)
                {
                    path = Path.Combine(RootPath, _currentCategory.Value.Name);

                    if (_currentSubPath != null)
                        path = Path.Combine(RootPath, _currentCategory.Value.Name, _currentSubPath);
                }

                if (string.IsNullOrEmpty(fileName.Text))
                    path = Path.Combine(path, "New folder");
                else if (char.IsDigit(fileName.Text[0]))
                    path = Path.Combine(path, "_" + fileName.Text);
                else
                    path = Path.Combine(path, fileName.Text);

                path = IncrementFolderIfExists(path);

                Directory.CreateDirectory(path);

                if (pathProvided)
                    CreateFileSystemEntryTilesAsync();

                Refresh();
            }
        }

        private async void ContentDialogRename(string path)
        {
            TextBox fileName;

            var dialog = new ContentDialog()
            {
                XamlRoot = _files.XamlRoot,
                Title = "Rename",
                PrimaryButtonText = "Rename",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Primary,
                Content = fileName = new TextBox() { Text = Path.GetFileNameWithoutExtension(path) },
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

                if (string.IsNullOrEmpty(Path.GetExtension(path)))
                    Directory.Move(path, Path.Combine(GoUpDirectory(path), fileName.Text));
                else
                {
                    //await RenameInsideFile(path, fileName.Text);

                    File.Move(path, Path.Combine(GoUpDirectory(path), fileName.Text) + Path.GetExtension(path));
                }

                CreateFileSystemEntryTilesAsync();

                //PropertiesController.Clear();
                //PropertiesController.Set(new Properties(path));
            }
        }

        private async void ContentDialogDelete(string path)
        {
            var dialog = new ContentDialog()
            {
                XamlRoot = _files.XamlRoot,
                Title = "Delete " + Path.GetFileName(path),
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrEmpty(Path.GetExtension(path)))
                    DeleteDirectory(path);
                else
                    File.Delete(path);

                CreateFileSystemEntryTilesAsync();
            }
        }

        private async Task WriteFileFromTemplatesAsync(string path)
        {
            string templatePath = Path.Combine(AppContext.BaseDirectory, TEMPLATES, _currentCategory.Value.Name + ".txt");

            using (FileStream fs = File.Create(path))
                if (File.Exists(templatePath))
                {
                    // writing data in string
                    string text = await File.ReadAllTextAsync(templatePath);

                    string fileName = Path.GetFileNameWithoutExtension(path);

                    if (text.Contains("{{FileName}}"))
                        text = text.Replace("{{FileName}}", Regex.Replace(fileName, @"[\s+\(\)]", ""));

                    byte[] info = new UTF8Encoding(true).GetBytes(text);

                    fs.Write(info, 0, info.Length);
                    fs.Close();
                }
        }

        private void SetBreadcrumbBar()
        {
            if (_currentCategory is null)
                Bar.ItemsSource = new string[] { };
            else
            {
                var source = new string[] { "Assets", _currentCategory.Value.Name };

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
    }

    internal partial class FilesController
    {
        private static readonly string TEMPLATES = @"Assets\Engine\Resources\Templates";

        public void OnDragOver(DragEventArgs e)
        {
            e.AcceptedOperation = DataPackageOperation.Copy;

            if (e.DragUIOverride != null)
            {
                e.DragUIOverride.Caption = "Add file(s)";
                e.DragUIOverride.IsContentVisible = true;
            }
        }

        public async void OnDropAsync(DragEventArgs e)
        {
            if (e.DataView.Contains(StandardDataFormats.StorageItems))
            {
                var items = await e.DataView.GetStorageItemsAsync();
                if (items.Count > 0)
                    foreach (var file in items.OfType<StorageFile>())
                        AddFileSystemEntry(file);
            }
        }

        public async void PasteFileSystemEntryFromClipboard(string path)
        {
            DataPackageView dataPackageView = Clipboard.GetContent();
            if (dataPackageView.Contains(StandardDataFormats.Text))
            {
                var sourcePath = await dataPackageView.GetTextAsync();
                var sourcePathCatagory = Path.GetRelativePath(RootPath, sourcePath).Split("\\").First();
                var targetPathCatagory = Path.GetRelativePath(RootPath, path).Split("\\").First();

                if (sourcePathCatagory == targetPathCatagory)
                    if (string.IsNullOrEmpty(Path.GetExtension(sourcePath)))
                        PasteFolder(sourcePath, GetDirectory(path), dataPackageView.RequestedOperation);
                    else
                        PasteFile(sourcePath, GetDirectory(path), dataPackageView.RequestedOperation);

                Refresh();
            }
        }

        public void OpenFolder(string path)
        {
            if (Directory.Exists(path))
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }

        public void OpenFile(string path)
        {
            if (File.Exists(path))
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }

        public void CopyDirectory(string sourcePath, string targetPath, bool deleteSourcePath = false)
        {
            // Create the target directory
            Directory.CreateDirectory(targetPath = IncrementFolderIfExists(Path.Combine(targetPath, Path.GetFileName(sourcePath))));

            // Now Create all of the directories
            foreach (string dirPath in Directory.GetDirectories(sourcePath, "*", SearchOption.AllDirectories))
                Directory.CreateDirectory(IncrementFolderIfExists(Path.Combine(targetPath, Path.GetRelativePath(sourcePath, dirPath))));

            // Copy all the files & Replaces any files with the same name
            foreach (string filePath in Directory.GetFiles(sourcePath, "*.*", SearchOption.AllDirectories))
                File.Copy(filePath, IncrementFileIfExists(Path.Combine(targetPath, Path.GetRelativePath(sourcePath, filePath))), true);

            // Delete source directory after it is finished copying
            if (deleteSourcePath)
            {
                if (!targetPath.Contains(sourcePath))
                    DeleteDirectory(sourcePath);

                Refresh();
            }
        }

        public void DeleteDirectory(string path)
        {
            string[] files = Directory.GetFiles(path);
            string[] dirs = Directory.GetDirectories(path);

            foreach (string file in files)
                File.Delete(file);

            foreach (string dir in dirs)
                DeleteDirectory(dir);

            Directory.Delete(path, false);
        }

        public void PasteFolder(string sourcePath, string targetPath, DataPackageOperation requestedOperation)
        {
            if (Directory.Exists(sourcePath))
                if (requestedOperation == DataPackageOperation.Copy)
                    CopyDirectory(sourcePath, targetPath);
                else if (requestedOperation == DataPackageOperation.Move)
                    CopyDirectory(sourcePath, targetPath, true);
        }

        public void PasteFile(string sourcePath, string targetPath, DataPackageOperation requestedOperation)
        {
            if (File.Exists(sourcePath))
                if (requestedOperation == DataPackageOperation.Copy)
                    File.Copy(sourcePath, IncrementFileIfExists(Path.Combine(targetPath, Path.GetFileName(sourcePath))), true);
                else if (requestedOperation == DataPackageOperation.Move && targetPath != GoUpDirectory(sourcePath))
                    File.Move(sourcePath, IncrementFileIfExists(Path.Combine(targetPath, Path.GetFileName(sourcePath))), true);
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

        private string GetDirectory(string path)
        {
            if (!string.IsNullOrEmpty(Path.GetExtension(path)))
                path = GoUpDirectory(path);

            return path;
        }

        private MenuFlyout CreateDefaultMenuFlyout(string path, bool hasExtension = false)
        {
            MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Create File System Entry", Icon = new SymbolIcon(Symbol.NewFolder) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Open", Icon = new SymbolIcon(Symbol.OpenFile) },
                new MenuFlyoutItem() { Text = "Show in Explorer", Icon = new FontIcon(){ Glyph = "\xEC50", FontFamily = new FontFamily("Segoe MDL2 Assets") } },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Cut", Icon = new SymbolIcon(Symbol.Cut) },
                new MenuFlyoutItem() { Text = "Copy", Icon = new SymbolIcon(Symbol.Copy) },
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Copy Path", Icon = new SymbolIcon(Symbol.Copy) },
            };

            //items[0].KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.X, Modifiers = VirtualKeyModifiers.Control });
            items[0].Click += (s, e) => ContentDialogCreateNewFileOrFolderAndRefreshAsync(path);

            if(hasExtension)
                items[1].Click += (s, e) => OpenFile(path);
            else
                items[1].Click += (s, e) => GoIntoDirectoryAndRefresh(path);
            items[2].Click += (s, e) => OpenFolder(hasExtension ? GoUpDirectory(path) : path);

            items[3].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.Move);
            items[4].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.Copy);
            items[5].Click += (s, e) => PasteFileSystemEntryFromClipboard(path);

            items[6].Click += (s, e) => ContentDialogRename(path);
            items[7].Click += (s, e) => ContentDialogDelete(path);

            items[8].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.None);

            MenuFlyout menuFlyout = new();
            foreach (var item in items)
            {
                menuFlyout.Items.Add(item);

                if (item.Text == "Create File System Entry"
                    || item.Text == "Show in Explorer"
                    || item.Text == "Paste")
                    menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }

            return menuFlyout;
        }

        private MenuFlyout CreateRootMenuFlyout(string path)
        {
            MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Create File System Entry", Icon = new SymbolIcon(Symbol.NewFolder) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Show in Explorer", Icon = new FontIcon(){ Glyph = "\xEC50", FontFamily = new FontFamily("Segoe MDL2 Assets") } },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Cut", Icon = new SymbolIcon(Symbol.Cut) },
                new MenuFlyoutItem() { Text = "Copy", Icon = new SymbolIcon(Symbol.Copy) },
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Copy Path", Icon = new SymbolIcon(Symbol.Copy) },
            };
                        
            //items[0].KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.X, Modifiers = VirtualKeyModifiers.Control });
            items[0].Click += (s, e) => ContentDialogCreateNewFileOrFolderAndRefreshAsync(path);

            items[1].Click += (s, e) => OpenFolder(GoUpDirectory(path));

            items[2].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.Move);
            items[3].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.Copy);
            items[4].Click += (s, e) => PasteFileSystemEntryFromClipboard(path);

            items[5].Click += (s, e) => ContentDialogRename(path);
            items[6].Click += (s, e) => ContentDialogDelete(path);

            items[7].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.None);

            MenuFlyout menuFlyout = new();
            foreach (var item in items)
            {
                if (string.IsNullOrEmpty(_currentSubPath))
                    if (item.Text == "Cut" || item.Text == "Rename")
                        continue;

                menuFlyout.Items.Add(item);

                if (item.Text == "Create File System Entry"
                    || item.Text == "Show in Explorer"
                    || item.Text == "Paste")
                    menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }

            return menuFlyout;
        }

        private Grid CreateIcon(Category _category)
        {
            Grid grid = new Grid();

            dynamic icon;

            if (string.IsNullOrEmpty(_category.Glyph))
                icon = CreateIcon(_category.Symbol);
            else
                icon = CreateIcon(_category.Glyph);

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

        private async void PreviewfileToImageAsync(StorageFile file, Image image)
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

        private async void RenameInsideFile(string path, string newFileName)
        {
            if (File.Exists(path))
                using (FileStream fs = File.Open(path, FileMode.Open))
                {
                    // writing data in string
                    string text = await File.ReadAllTextAsync(path);

                    string fileName = Path.GetFileNameWithoutExtension(path);

                    if (text.Contains(fileName))
                        text = text.Replace(fileName, Regex.Replace(newFileName, @"[\s+\(\)]", ""));

                    byte[] info = new UTF8Encoding(true).GetBytes(text);

                    fs.Write(info, 0, info.Length);
                    fs.Close();
                }
        }

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

        private string RemoveLastChar(string s) { return s.Remove(s.Length - 1); }

        private void CopyToClipboard(string path, DataPackageOperation requestedOpertion)
        {
            DataPackage data = new();
            data.SetText(path);
            data.RequestedOperation = requestedOpertion;

            Clipboard.SetContent(data);
        }
    }
}