using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;
using System.Collections.Generic;
using Windows.Storage;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Files : UserControl
{
    public Grid ChangeColorWithTheme;

    public List<StorageFile> DragDropFiles = new();

    internal Controller.Files _filesControl;

    public Files()
    {
        this.InitializeComponent();

        ChangeColorWithTheme = x_Grid_Main;

        _filesControl = new(this, x_Grid_Main, x_WrapPanel_Files, x_BreadcrumBar_Files);
    }

    private void AppBarButton_Click_AddFiles(object sender, RoutedEventArgs e) =>
        _filesControl.SelectFilesAsync();

    private void AppBarButton_Click_OpenFolder(object sender, RoutedEventArgs e) =>
        _filesControl.OpenFolder();

    private void AppBarButton_Click_RefreshFiles(object sender, RoutedEventArgs e) =>
        _filesControl.Refresh();
    
    private void AppBarButton_Click_OpenVisualStudio(object sender, RoutedEventArgs e) =>
        _filesControl.OpenFile(System.IO.Path.Combine(Controller.Home.ProjectPath, "Project.sln"));

    private void BreadcrumBar_Files_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args) =>
        _filesControl.GoUpDirectoryAndRefresh();

    private void Grid_Main_DragOver(object sender, DragEventArgs e) =>
        _filesControl.OnDragOver(e);

    private void Grid_Main_Drop(object sender, DragEventArgs e) =>
        _filesControl.OnDropAsync(e);
}
