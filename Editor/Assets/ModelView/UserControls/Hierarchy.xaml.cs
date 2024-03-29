﻿using Microsoft.UI.Xaml.Controls;

// To learn more about WinUI, the WinUI project structure,
// and more about our project templates, see: http://aka.ms/winui-project-info.

namespace Editor.ModelView;

public sealed partial class Hierarchy : UserControl
{
    internal Controller.Hierarchy HierarchyControl;

    public Hierarchy()
    {
        this.InitializeComponent();

        HierarchyControl = new(this, x_StackPanel_Hierarchy);
    }

    private void AppBarButton_SwitchLayout_Click(object sender, Microsoft.UI.Xaml.RoutedEventArgs e) =>
        Controller.Main.Instance.LayoutControl.SwitchPaneLayout();
}
