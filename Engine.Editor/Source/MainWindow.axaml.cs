using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Editor;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        AvaloniaXamlLoader.Load(this);
    }
}