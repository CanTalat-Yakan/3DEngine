using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading.Tasks;
using System;
using Windows.ApplicationModel.DataTransfer;
using Windows.Storage.Pickers;
using Windows.Storage.Streams;
using Windows.Storage;
using Windows.UI;

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

    internal partial class Files
    {
        public string ProjectPath { get; private set; }

        public Grid Content;
        public BreadcrumbBar Bar;
        public WrapPanel Wrap;

        private ModelView.Files _files;

        public Category[] Categories;

        private Category? _currentCategory;
        private string _currentSubPath;

        public Files(ModelView.Files files, Grid grid, WrapPanel wrap, BreadcrumbBar bar)
        {
            // Assign local variables.
            Content = grid;
            Wrap = wrap;
            Bar = bar;

            _files = files;

            // Assign the ProjectPath value from static property in "Home".
            ProjectPath = Home.ProjectPath;

            // Call the method to initialize and populate the files categories with a DataTemplate.
            PopulateFilesCategories();
        }

        public void PopulateFilesCategories()
        {
            // Create a list of categories presented with attrubutes Name, Smybol or Glyph, FileType, Creatable and Thumbnail.
            CreateCatergoryTiles(
                new() { Name = "Scenes", Glyph = "\xEA86", FileTypes = new string[] { ".usd", ".usda", ".usdc", ".usdz" }, Creatable = true },
                new() { Name = "Scripts", Symbol = Symbol.Document, FileTypes = new string[] { ".cs" }, Creatable = true },
                new() { Name = "Prefabs", Glyph = "\xE734", FileTypes = new string[] { ".prefab" } },
                new() { Name = "Models", Glyph = "\xF158", FileTypes = new string[] { ".fbx", ".obj", ".blend", ".3ds", ".dae" } },
                new() { Name = "Animations", Glyph = "\xE805", FileTypes = new string[] { ".fbx", ".dae" } },
                new() { Name = "Materials", Glyph = "\xF156", FileTypes = new string[] { ".mat" }, Creatable = true },
                new() { Name = "Textures", Symbol = Symbol.Pictures, FileTypes = new string[] { ".png", ".jpg", ".jpeg", ".tiff", ".tga", ".psd", ".bmp", }, Thumbnail = true },
                new() { Name = "Audios", Symbol = Symbol.Audio, FileTypes = new string[] { ".m4a", ".mp3", ".wav", ".ogg" } },
                new() { Name = "Videos", Symbol = Symbol.Video, FileTypes = new string[] { ".m4v", ".mp4", ".mov", ".avi" }, Thumbnail = false },
                new() { Name = "Fonts", Symbol = Symbol.Font, FileTypes = new string[] { ".ttf", ".otf" } },
                new() { Name = "Shaders", Glyph = "\xE706", FileTypes = new string[] { ".hlsl" }, Creatable = true },
                new() { Name = "Documents", Symbol = Symbol.Document, FileTypes = new string[] { ".txt", ".pdf", ".doc", ".docx" }, Creatable = true },
                new() { Name = "Packages", Glyph = "\xE7B8", FileTypes = new string[] { ".zip", ".7zip", ".rar" } });
        }

        public async void SelectFilesAsync()
        {
            // Validate the categories exist to make sure they are set up properly.
            ValidateCategoriesExist();

            // Create a new instance of the FileOpenPicker.
            var picker = new FileOpenPicker()
            {
                // Set the view mode to thumbnail view.
                ViewMode = PickerViewMode.Thumbnail,
                // Set the suggested start location to the desktop.
                SuggestedStartLocation = PickerLocationId.Desktop,
            };

            // If the current category is not null, add the file types of the category to the filter.
            if (_currentCategory != null)
                foreach (var type in _currentCategory.Value.FileTypes)
                    picker.FileTypeFilter.Add(type);
            else
                // Otherwise, add all file types to the filter.
                picker.FileTypeFilter.Add("*");

            // Make sure to get the HWND from a Window object,
            // pass a Window reference to GetWindowHandle
            // and initialize picker with handle.
            var hwnd = WinRT.Interop.WindowNative.GetWindowHandle((Application.Current as App)?.Window as MainWindow);
            WinRT.Interop.InitializeWithWindow.Initialize(picker, hwnd);

            // Pick multiple files using the picker and store the result in "files".
            var files = await picker.PickMultipleFilesAsync();

            // Loop through all the picked files.
            foreach (StorageFile file in files)
                // If a file was picked, loop through all the categories and check if the file type matches.
                if (file != null)
                    foreach (var category in Categories)
                        foreach (var type in category.FileTypes)
                            if (type == file.FileType)
                            {
                                // Create the target path by combining the project path, category name, and file name.
                                string targetPath = Path.Combine(ProjectPath, category.Name);
                                targetPath = Path.Combine(targetPath, file.Name);
                                // Copy the file to the target path and overwrite if it already exists. 
                                File.Copy(file.Path, targetPath, true);
                            }

            // Call the refresh method to update the category and file list.
            Refresh();
        }

        public void AddFileSystemEntry(StorageFile file)
        {
            // Check if the file is null or the file type is empty.
            if (file is null || string.IsNullOrEmpty(file.FileType))
                return;

            // Loop through all categories to find a match with the file type.
            foreach (var category in Categories)
                foreach (var type in category.FileTypes)
                    // Check if the file type matches the type in the current category.
                    if (type == file.FileType)
                    {
                        // Create the target path by combining the project path and the name of the matching category.
                        string targetPath = Path.Combine(ProjectPath, category.Name);

                        // If a currently in a subpath, check if its name matches the targetPath for the file.
                        if (_currentCategory != null)
                            if (_currentCategory.Value.Name == category.Name)
                                if (!string.IsNullOrEmpty(_currentSubPath))
                                    targetPath = Path.Combine(targetPath, _currentSubPath);

                        // Call the method "PasteFile" with the file path, target path, and the copy operation.
                        PasteFile(file.Path, targetPath, DataPackageOperation.Copy);
                    }

            // Call the refresh method to update the category and file list.
            Refresh();
        }

        public void GoUpDirectoryAndRefresh()
        {
            // Check if the current sub-path is not empty.
            if (!string.IsNullOrEmpty(_currentSubPath))
            {
                // If the current sub-path is not empty, go up one directory level using the GoUpDirectory method.
                _currentSubPath = GoUpDirectory(_currentSubPath);

                // Call the refresh method to update the category and file list.
                Refresh();
            }
            else
            {
                // If the current sub-path is empty, set the current category and sub-path to null.
                _currentCategory = null;
                _currentSubPath = null;

                // Create category tiles based on the Categories list.
                CreateCatergoryTiles(Categories);
            }
        }

        private void GoIntoDirectoryAndRefresh(string path)
        {
            // Set the current sub-directory path to the relative path of the given path
            // with respect to the current category's directory in the project path.
            _currentSubPath = Path.GetRelativePath(
                Path.Combine(ProjectPath, _currentCategory.Value.Name),
                path);

            // Call the refresh method to update the category and file list.
            Refresh();
        }

        public void OpenFolder()
        {
            // Assign local variable to the ProjectPath.
            var path = ProjectPath;

            // If the current category exists, combine it with the ProjectPath and set it as to the path.
            if (_currentCategory != null)
            {
                // Create the full path to the current category folder.
                path = Path.Combine(ProjectPath, _currentCategory.Value.Name);

                // If there is a current sub-path, add it to the path.
                if (!string.IsNullOrEmpty(_currentSubPath))
                    path = Path.Combine(path, _currentSubPath);
            }

            // Check if the folder at the path exists.
            if (Directory.Exists(path))
                // If the folder exists, start a process to open it in the default file explorer
                // UseShellExecute is set to "true" to run the process with elevated privileges.
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
        }

        public void Refresh()
        {
            // Validate if the categories exist and their correct file types exist.
            ValidateCategoriesExist();
            ValidateCorrectFileTypes();

            // If the current category is not set, create the category tiles.
            if (_currentCategory is null)
                CreateCatergoryTiles(Categories);
            // If currently inside category, create the file system entry tiles.
            else
                CreateFileSystemEntryTilesAsync();

            // Set the breadcrumb bar with the correct values.
            SetBreadcrumbBar();
        }

        public void ValidateCategoriesExist()
        {
            string categoryPath;

            foreach (var category in Categories)
                if (!Directory.Exists(categoryPath = Path.Combine(ProjectPath, category.Name)))
                    Directory.CreateDirectory(categoryPath);
        }

        public void ValidateCorrectFileTypes()
        {
            bool dirty = false;

            if (_currentCategory is null)
                return;

            var targetPath = Path.Combine(ProjectPath, _currentCategory.Value.Name);
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
                                    IncrementFileIfExists(Path.Combine(ProjectPath, category2.Name, Path.GetFileName(path))));

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

            string currentPath = Path.Combine(ProjectPath, _currentCategory.Value.Name);

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
                        currentPath = Path.Combine(ProjectPath, _currentCategory.Value.Name);
                        break;
                    }

                    currentPath = Path.Combine(ProjectPath, _currentCategory.Value.Name, _currentSubPath);
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

                Image image = new() { Width = 145, Height = 90 };

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
            Grid grid = new() { Padding = new(-1), CornerRadius = new(10) };
            grid.Background = new SolidColorBrush(new Color()
            {
                A = 255,
                R = (byte)new Random().Next(32, 96),
                B = (byte)new Random().Next(32, 96),
                G = (byte)new Random().Next(32, 96)
            });

            Button button = new()
            {
                Width = 145,
                Height = 90,
                Padding = new(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
                DataContext = category,
            };
            button.Click += (s, e) =>
            {
                _currentCategory = (Category)((Button)e.OriginalSource).DataContext;

                Refresh();
            };

            Grid grid2 = new() { HorizontalAlignment = HorizontalAlignment.Stretch };

            Viewbox viewbox = new() { MaxHeight = 24, MaxWidth = 24, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            TextBlock label = new() { Text = category.Name, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };

            viewbox.Child = icon;
            grid2.Children.Add(viewbox);
            grid2.Children.Add(label);
            button.Content = grid2;
            grid.Children.Add(button);

            Content.ContextFlyout = null;

            return grid;
        }

        private Grid FolderTile(string path, Grid icon)
        {
            Grid grid = new() { Padding = new(-1), CornerRadius = new(10) };
            grid.Background = new SolidColorBrush(new Color()
            {
                A = 255,
                R = (byte)new Random().Next(32, 96),
                B = (byte)new Random().Next(32, 96),
                G = (byte)new Random().Next(32, 96)
            });

            Button button = new()
            {
                Width = 145,
                Height = 75,
                Padding = new(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            button.ContextFlyout = CreateDefaultMenuFlyout(path);
            button.Click += (s, e) => GoIntoDirectoryAndRefresh(path);

            Grid grid2 = new() { HorizontalAlignment = HorizontalAlignment.Stretch };

            Viewbox viewbox = new() { MaxHeight = 24, MaxWidth = 24, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            TextBlock label = new()
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

            Grid grid = new() { Margin = new(0, 0, 0, -30) };
            Grid grid2 = new() { Padding = new(10) };
            Grid grid3 = new();

            Viewbox viewbox = new() { MaxHeight = 24, MaxWidth = 24, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Top };
            TextBlock fileType = new() { Text = Path.GetExtension(path), HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Bottom };

            StackPanel stack = new() { Spacing = 5 };

            Button button = new()
            {
                Width = 143,
                Height = 73,
                Padding = new(0),
                CornerRadius = new(10),
                HorizontalContentAlignment = HorizontalAlignment.Stretch,
                VerticalContentAlignment = VerticalAlignment.Stretch,
            };
            button.ContextFlyout = CreateDefaultMenuFlyout(path, true);
            button.Tapped += (s, e) =>
            {
                Properties.Clear();
                Properties.Set(new ModelView.Properties(path));
            };
            button.DoubleTapped += (s, e) =>
            {
                if (File.Exists(path))
                    Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            };

            TextBlock label = new()
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
            var path = Path.Combine(ProjectPath, _currentCategory.Value.Name);
            if (!string.IsNullOrEmpty(_currentSubPath))
                path = Path.Combine(path, _currentSubPath);

            Content.ContextFlyout = CreateRootMenuFlyout(path);

            Grid grid = new();

            Button button = new()
            {
                Width = 67,
                Height = 73,
                CornerRadius = new(10),
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
            };
            button.Click += (s, e) =>
            {
                GoUpDirectoryAndRefresh();
            };

            Viewbox viewbox = new() { MaxHeight = 24, MaxWidth = 24 };

            viewbox.Child = icon;
            button.Content = viewbox;
            grid.Children.Add(button);

            return grid;
        }

        private Grid AddTile(Grid icon)
        {
            Grid grid = new();

            Button button = new()
            {
                Width = 66,
                Height = 73,
                CornerRadius = new(10),
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

            Viewbox viewbox = new() { MaxHeight = 24, MaxWidth = 24 };

            viewbox.Child = icon;
            button.Content = viewbox;
            grid.Children.Add(button);

            return grid;
        }

        private async void ContentDialogCreateNewFileOrFolderAndRefreshAsync(string path = "")
        {
            ContentDialog dialog = new()
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

            ContentDialog dialog = new()
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
                        new ContentDialog()
                        {
                            XamlRoot = _files.XamlRoot,
                            Title = "A file can't contain any of the following characters",
                            CloseButtonText = "Close",
                            DefaultButton = ContentDialogButton.Close,
                            Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                        }.CreateDialogAsync();

                        return;
                    }

                var pathProvided = string.IsNullOrEmpty(path);
                if (pathProvided)
                {
                    path = Path.Combine(ProjectPath, _currentCategory.Value.Name);

                    if (_currentSubPath != null)
                        path = Path.Combine(ProjectPath, _currentCategory.Value.Name, _currentSubPath);
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

                Properties.Clear();
                Properties.Set(new ModelView.Properties(path));
            }
        }

        private async void ContentDialogCreateNewFolderAndRefreshAsync(string path = "")
        {
            TextBox fileName;

            ContentDialog dialog = new()
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
                        new ContentDialog()
                        {
                            XamlRoot = _files.XamlRoot,
                            Title = "A folder can't contain any of the following characters",
                            CloseButtonText = "Close",
                            DefaultButton = ContentDialogButton.Close,
                            Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                        }.CreateDialogAsync();

                        return;
                    }

                var pathProvided = string.IsNullOrEmpty(path);
                if (pathProvided)
                {
                    path = Path.Combine(ProjectPath, _currentCategory.Value.Name);

                    if (_currentSubPath != null)
                        path = Path.Combine(ProjectPath, _currentCategory.Value.Name, _currentSubPath);
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

            ContentDialog dialog = new()
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
                        new ContentDialog()
                        {
                            XamlRoot = _files.XamlRoot,
                            Title = "A folder can't contain any of the following characters",
                            CloseButtonText = "Close",
                            DefaultButton = ContentDialogButton.Close,
                            Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                        }.CreateDialogAsync();

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
            ContentDialog dialog = new()
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

    internal partial class Files
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
                var sourcePathCatagory = Path.GetRelativePath(ProjectPath, sourcePath).Split("\\").First();
                var targetPathCatagory = Path.GetRelativePath(ProjectPath, path).Split("\\").First();

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
                new MenuFlyoutItem() { Text = "Create file system entry", Icon = new SymbolIcon(Symbol.NewFolder) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Open file", Icon = new SymbolIcon(Symbol.OpenFile) },
                new MenuFlyoutItem() { Text = "Open folder location", Icon = new FontIcon(){ Glyph = "\xEC50", FontFamily = new FontFamily("Segoe MDL2 Assets") } },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Cut", Icon = new SymbolIcon(Symbol.Cut) },
                new MenuFlyoutItem() { Text = "Copy", Icon = new SymbolIcon(Symbol.Copy) },
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Copy as Path", Icon = new SymbolIcon(Symbol.Copy) },
            };

            //items[0].KeyboardAccelerators.Add(new KeyboardAccelerator() { Key = VirtualKey.X, Modifiers = VirtualKeyModifiers.Control });
            items[0].Click += (s, e) => ContentDialogCreateNewFileOrFolderAndRefreshAsync(path);

            if (hasExtension)
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

                if (item.Text == "Create file system entry"
                    || item.Text == "Open folder location"
                    || item.Text == "Paste"
                    || item.Text == "Delete")
                    menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }

            return menuFlyout;
        }

        private MenuFlyout CreateRootMenuFlyout(string path)
        {
            MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Create file system entry", Icon = new SymbolIcon(Symbol.NewFolder) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Open folder location", Icon = new FontIcon(){ Glyph = "\xEC50", FontFamily = new FontFamily("Segoe MDL2 Assets") } },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Cut", Icon = new SymbolIcon(Symbol.Cut) },
                new MenuFlyoutItem() { Text = "Copy", Icon = new SymbolIcon(Symbol.Copy) },
                new MenuFlyoutItem() { Text = "Paste", Icon = new SymbolIcon(Symbol.Paste) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Copy as Path", Icon = new SymbolIcon(Symbol.Copy) },
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

                if (item.Text == "Create file system entry"
                    || item.Text == "Open folder location"
                    || item.Text == "Paste"
                    || item.Text == "Delete")
                    menuFlyout.Items.Add(new MenuFlyoutSeparator());
            }

            return menuFlyout;
        }

        private Grid CreateIcon(Category _category)
        {
            Grid grid = new();

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
            Grid grid = new();

            FontIcon icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = glyph };

            grid.Children.Add(icon);

            return grid;
        }

        private Grid CreateIcon(Symbol symbol)
        {
            Grid grid = new();

            SymbolIcon symbolIcon = new() { Symbol = symbol };

            grid.Children.Add(symbolIcon);

            return grid;
        }

        private async void PreviewfileToImageAsync(StorageFile file, Image image)
        {
            using (IRandomAccessStream fileStream = await file.OpenAsync(FileAccessMode.Read))
            {
                BitmapImage bitmapImage = new() { DecodePixelWidth = (int)image.Width, DecodePixelHeight = (int)image.Height };

                await bitmapImage.SetSourceAsync(fileStream);

                image.Source = bitmapImage;
            }
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