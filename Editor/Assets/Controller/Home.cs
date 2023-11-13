using System.Diagnostics;
using System.IO.Compression;
using System.IO;
using System.Text.RegularExpressions;
using System;
using Windows.ApplicationModel.DataTransfer;

using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace Editor.Controller;

public sealed partial class Home
{
    public static string RootPath { get; private set; }
    public static string ProjectPath { get; private set; }

    private ModelView.Home _home;
    private WrapPanel _wrap;
    private NavigationView _navigationView;

    public Home(ModelView.Home home, WrapPanel wrap, NavigationView navigationView)
    {
        _home = home;
        _wrap = wrap;
        _navigationView = navigationView;

        var documentsDir = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        RootPath = Path.Combine(documentsDir, "3DEngine");

        PopulateProjectTiles();
    }

    public void OpenFolder()
    {
        if (Directory.Exists(RootPath))
            // If the RootPath exists, start a process to open it in the default file explorer
            // UseShellExecute is set to "true" to run the process with elevated privileges.
            Process.Start(new ProcessStartInfo { FileName = RootPath, UseShellExecute = true });
    }

    public void PopulateProjectTiles()
    {
        _wrap.Children.Clear();

        _wrap.Children.Add(AddTile(CreateIcon(Symbol.Add)));

        foreach (var projectPath in Directory.GetDirectories(RootPath))
            _wrap.Children.Add(ProjectTile(projectPath, CreateIcon("\xE74C")));
    }
}

public sealed partial class Home
{
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
        button.Click += (s, e) => ContentDialogCreateNewProjectAsync();

        Viewbox viewbox = new() { MaxHeight = 24, MaxWidth = 24 };

        viewbox.Child = icon;
        button.Content = viewbox;
        grid.Children.Add(button);

        return grid;
    }

    private Grid ProjectTile(string path, Grid icon)
    {
        Grid grid = new() { Padding = new(-1), CornerRadius = new(10) };

        Button button = new()
        {
            Width = 245,
            Height = 75,
            Padding = new(10),
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
        };
        button.ContextFlyout = CreateDefaultMenuFlyout(path);
        button.Click += (s, e) => OpenEngine(path);

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

}

public sealed partial class Home
{
    private async void ContentDialogCreateNewProjectAsync()
    {
        TextBox fileName;

        ContentDialog dialog = new()
        {
            XamlRoot = _home.XamlRoot,
            Title = "Create a new project",
            PrimaryButtonText = "Save",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = fileName = new TextBox() { PlaceholderText = "New Project" },
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
                        XamlRoot = _home.XamlRoot,
                        Title = "A project name can't contain any of the following characters",
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close,
                        Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                    }.CreateDialogAsync();

                    return;
                }

            var path = RootPath;

            if (string.IsNullOrEmpty(fileName.Text))
                path = Path.Combine(path, "New Project");
            else if (char.IsDigit(fileName.Text[0]))
                path = Path.Combine(path, "_" + fileName.Text);
            else
                path = Path.Combine(path, fileName.Text);

            path = IncrementFolderIfExists(path);

            Directory.CreateDirectory(path);
            Directory.CreateDirectory(Path.Combine(path, Engine.Paths.ASSETS));

            string zipPath = Path.Combine(AppContext.BaseDirectory, Engine.Paths.TEMPLATES, "Project", "Project.zip");
            if (File.Exists(zipPath))
                ZipFile.ExtractToDirectory(zipPath, path);

            string dllPath = Path.Combine(AppContext.BaseDirectory, Engine.Paths.TEMPLATES, "Project", "Engine.dll");
            if (File.Exists(dllPath))
                File.Copy(dllPath, Path.Combine(path, "Engine.dll"));

            PopulateProjectTiles();
        }
    }

    private async void ContentDialogRename(string path)
    {
        TextBox projectName;

        ContentDialog dialog = new()
        {
            XamlRoot = _home.XamlRoot,
            Title = "Rename",
            PrimaryButtonText = "Rename",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Primary,
            Content = projectName = new TextBox() { Text = Path.GetFileName(path) },
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            // \w is equivalent of [0 - 9a - zA - Z_]."
            if (!string.IsNullOrEmpty(projectName.Text))
                if (!Regex.Match(projectName.Text, @"^[\w\-.]+$").Success)
                {
                    new ContentDialog()
                    {
                        XamlRoot = _home.XamlRoot,
                        Title = "A folder can't contain any of the following characters",
                        CloseButtonText = "Close",
                        DefaultButton = ContentDialogButton.Close,
                        Content = new TextBlock() { Text = "\\ / : * ? \" < > |" },
                    }.CreateDialogAsync();

                    return;
                }


            Directory.Move(path, Path.Combine(RootPath, projectName.Text));

            PopulateProjectTiles();
        }
    }

    private async void ContentDialogDelete(string path)
    {
        ContentDialog dialog = new()
        {
            XamlRoot = _home.XamlRoot,
            Title = "Delete " + Path.GetFileName(path),
            PrimaryButtonText = "Delete",
            CloseButtonText = "Cancel",
            DefaultButton = ContentDialogButton.Close,
        };

        var result = await dialog.ShowAsync();

        if (result == ContentDialogResult.Primary)
        {
            dialog = new()
            {
                XamlRoot = _home.XamlRoot,
                Title = "Delete " + Path.GetFileName(path) + " permanently?",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
                Content = new TextBlock() {
                    Text = "If you delete this project, you won't be able to recover it. \nDo you want to proceed?",
                    TextWrapping = TextWrapping.WrapWholeWords },
            };

            result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                DeleteDirectory(path);

                PopulateProjectTiles();
            }
        }
    }

    private MenuFlyout CreateDefaultMenuFlyout(string path, bool hasExtension = false)
    {
        MenuFlyoutItem[] items = new[] {
                new MenuFlyoutItem() { Text = "Open project", Icon = new SymbolIcon(Symbol.OpenFile) },
                new MenuFlyoutItem() { Text = "Open folder location", Icon = new FontIcon(){ Glyph = "\xEC50", FontFamily = new FontFamily("Segoe MDL2 Assets") } },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Rename", Icon = new SymbolIcon(Symbol.Rename) },
                new MenuFlyoutItem() { Text = "Delete", Icon = new SymbolIcon(Symbol.Delete) },
                //new MenuFlyoutSeparator(),
                new MenuFlyoutItem() { Text = "Copy as Path", Icon = new SymbolIcon(Symbol.Copy) },
            };

        items[0].Click += (s, e) => OpenEngine(path);
        items[1].Click += (s, e) => OpenFolder(path);

        items[2].Click += (s, e) => ContentDialogRename(path);
        items[3].Click += (s, e) => ContentDialogDelete(path);

        items[4].Click += (s, e) => CopyToClipboard(path, DataPackageOperation.None);

        MenuFlyout menuFlyout = new();
        foreach (var item in items)
        {
            menuFlyout.Items.Add(item);

            if (item.Text == "Open folder location"
                || item.Text == "Delete")
                menuFlyout.Items.Add(new MenuFlyoutSeparator());
        }

        return menuFlyout;
    }
}

public sealed partial class Home
{
    public void OpenFolder(string path)
    {
        if (Directory.Exists(path))
            Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void OpenEngine(string path)
    {
        ProjectPath = path;

        NavigationViewItem menuItem = new()
        {
            Tag = "engine",
            Name = Path.GetFileName(path),
            Content = Path.GetFileName(path),
            Icon = new FontIcon() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = "\xE74C" }
        };

        TeachingTip teachingTip = new()
        {
            Target = (FrameworkElement)menuItem,
            Title = "Click here to open the project",
            Subtitle = "The editor will load with the engine render pipeline"
        };

        _navigationView.MenuItems.Add(menuItem);
        //_navigationView.SelectedItem = menuItem;

        //teachingTip.IsOpen = true;
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

        foreach (var menuItem in _navigationView.MenuItems)
            if (menuItem.GetType().Equals(typeof(NavigationViewItem)))
                if (((NavigationViewItem)menuItem).Name == Path.GetFileName(path))
                {
                    _navigationView.MenuItems.Remove(menuItem);

                    break;
                }
    }

    private string IncrementFolderIfExists(string path)
    {
        var fileCount = 0;

        while (Directory.Exists(
            Path.Combine(
                RootPath,
                Path.GetFileName(path) +
                    (fileCount > 0
                    ? " (" + (fileCount + 1).ToString() + ")"
                    : ""))))
            fileCount++;

        return Path.Combine(
            RootPath,
            Path.GetFileName(path) +
                (fileCount > 0
                ? " (" + (fileCount + 1).ToString() + ")"
                : ""));
    }

    private void CopyToClipboard(string path, DataPackageOperation requestedOpertion)
    {
        DataPackage data = new();
        data.SetText(path);
        data.RequestedOperation = requestedOpertion;

        Clipboard.SetContent(data);
    }
}
