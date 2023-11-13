using System.Collections.Generic;
using Windows.Storage;

using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Files : UserControl
{
    public Grid ChangeColorWithTheme;

    public List<StorageFile> DragDropFiles = new();

    internal Controller.Files FilesControl;

    public Files()
    {
        this.InitializeComponent();

        ChangeColorWithTheme = x_Grid_Main;

        FilesControl = new(this, x_Grid_Main, x_WrapPanel_Files, x_BreadcrumbBar_Files);
    }

    private void AppBarButton_Click_AddFiles(object sender, RoutedEventArgs e) =>
        FilesControl.SelectFilesAsync();

    private void AppBarButton_Click_OpenFolder(object sender, RoutedEventArgs e) =>
        FilesControl.OpenFolder();

    private void AppBarButton_Click_RefreshFiles(object sender, RoutedEventArgs e) =>
        FilesControl.Refresh();
    
    private void AppBarButton_Click_OpenVisualStudio(object sender, RoutedEventArgs e) =>
        FilesControl.OpenFile(System.IO.Path.Combine(Controller.Home.ProjectPath, "Project.sln"));
    //Breadcrumb
    private void BreadcrumbBar_Files_ItemClicked(BreadcrumbBar sender, BreadcrumbBarItemClickedEventArgs args) =>
        FilesControl.GoUpDirectoryAndRefresh();

    private void Grid_Main_DragOver(object sender, DragEventArgs e) =>
        FilesControl.OnDragOver(e);

    private void Grid_Main_Drop(object sender, DragEventArgs e) =>
        FilesControl.OnDropAsync(e);
}
