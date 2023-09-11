using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System.Collections.Generic;

namespace Editor.Controller;

internal class GridDataTemeplate
{
    public GridLength Length = new(1, GridUnitType.Star);
    public double MinWidth = 1;
    public double MinHeight = 40;
    public UIElement Content;
}

internal partial class Layout
{
    public Grid MainContent;
    public Grid ContentRoot;
    public Grid PaneRoot;
    public SplitView SplitView;

    public ModelView.ViewPort ViewPort;
    public ModelView.Hierarchy Hierarchy;
    public ModelView.Properties Properties;
    public Grid PropertiesRoot;
    public ModelView.Output Output;
    public ModelView.Files Files;
    public Grid TabsRoot;

    public bool OneCollumnPaneLayout = true;
    public List<Grid> GridsToClear = new();

    public Layout(Grid content,
        ModelView.ViewPort viewPort,
        ModelView.Hierarchy hierarchy,
        ModelView.Properties properties,
        ModelView.Output output,
        ModelView.Files files)
    {
        MainContent = content;
        ViewPort = viewPort;
        Hierarchy = hierarchy;
        Properties = properties;
        Output = output;
        Files = files;

        // Initialize the PropertiesRoot property to a new instance.
        PropertiesRoot = new();
        // Add the Properties property to the Children collection of the PropertiesRoot property.
        PropertiesRoot.Children.Add(Properties);

        // Initialize the TabsRoot property to a new instance.
        TabsRoot = new();
    }

    public void CreateLayout()
    {
        // Create a new instance of the tab view and pass it the TabsRoot and the contents of the Files and Output tabs.
        var tabView = WrapInTabView(TabsRoot,
            new() { Content = Files, Header = "Files", Symbol = Symbol.Document },
            new() { Content = Output, Header = "Output", Symbol = Symbol.Message });

        // Create a new instance of the ContentRoot, and add the ViewPort wrapped in a grid and the tabView to it.
        ContentRoot = new();
        ContentRoot.Children.Add(PairVertical(
            new() { Content = WrapGrid(ViewPort), MinHeight = 0 },
            new() { Content = tabView, MinHeight = 0, Length = new(235, GridUnitType.Pixel) }));

        // Create a new instance of the PaneRoot and add the Hierarchy and PropertiesRoot wrapped in grids to it.
        PaneRoot = new();
        PaneRoot.Children.Add(PairVertical(
            new() { Content = WrapGrid(Hierarchy, GridsToClear), MinHeight = 0 },
            new() { Content = WrapGrid(PropertiesRoot, GridsToClear), MinHeight = 0, Length = new(1, GridUnitType.Star) },
            true, true));

        // Add the ContentRoot and PaneRoot wrapped in a split view to the MainContent.
        MainContent.Children.Add(WrapSplitView(ContentRoot, PaneRoot));
    }

    public void SwitchPaneLayout()
    {
        //Inverse bool variable OneCollumnPaneLayout that toggles between a one collumn and two collumn layout.
        OneCollumnPaneLayout = !OneCollumnPaneLayout;

        //Loop through all grids in GridsToClear and clear their children
        foreach (var grid in GridsToClear)
            grid.Children.Clear();

        //Clear the children of PaneRoot
        PaneRoot.Children.Clear();

        //Check if the layout is one collumn, if so add the Hierarchy and PropertiesRoot to PaneRoot vertically
        if (OneCollumnPaneLayout)
            PaneRoot.Children.Add(PairVertical(
                new() { Content = WrapGrid(Hierarchy, GridsToClear), MinHeight = 0 },
                new() { Content = WrapGrid(PropertiesRoot, GridsToClear), MinHeight = 0, Length = new(1, GridUnitType.Star) },
                true, true));
        //If not a one collumn layout, add the Hierarchy and PropertiesRoot to PaneRoot horizontally
        else
            PaneRoot.Children.Add(PairHorizontal(
                new() { Content = WrapGrid(Hierarchy, GridsToClear), MinWidth = 0 },
                new() { Content = WrapGrid(PropertiesRoot, GridsToClear), MinWidth = 0, Length = new(1, GridUnitType.Star) },
                false, true));

        //Set the OpenPaneLength of the SplitView to be 333 pixels if OneCollumnPaneLayout is true, and 666 pixels otherwise.
        SplitView.OpenPaneLength = OneCollumnPaneLayout ? 333 : 666;
    }
}

internal partial class Layout
{
    private Grid PairVertical(GridDataTemeplate top, GridDataTemeplate bottom, bool gridSplitter = true, bool seperator = false)
    {
        Grid grid = new();
        Grid grid2 = new();

        RowDefinition bottomRowDefinition;
        grid2.RowDefinitions.Add(new() { Height = top.Length, MinHeight = top.MinHeight });
        grid2.RowDefinitions.Add(bottomRowDefinition = new() { Height = bottom.Length, MinHeight = bottom.MinHeight });

        GridSplitter splitV = new() { VerticalAlignment = VerticalAlignment.Top, CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover };

        ((Grid)bottom.Content).Margin = new(0, 16, 0, 0);
        ((Grid)top.Content).Padding = new(0, 16, 0, 0);
        grid2.Padding = new(0, -16, 0, 0);

        grid2.Children.Add(top.Content);
        grid2.Children.Add(bottom.Content);
        Grid.SetRow((FrameworkElement)bottom.Content, 1);
        Grid.SetRow(splitV, 1);
        if (gridSplitter)
            grid2.Children.Add(splitV);

        AppBarSeparator separator = new() { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-2, 0, 0, 0) };
        grid.Children.Add(grid2);
        if (seperator)
            grid.Children.Add(separator);

        BindingOperations.SetBinding(bottomRowDefinition, RowDefinition.HeightProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new("IsChecked"), Converter = new BooleanToRowHeightConverter(bottom.Length) });

        return grid;
    }

    private Grid PairHorizontal(GridDataTemeplate left, GridDataTemeplate right, bool gridSplitter = true, bool seperator = false)
    {
        Grid grid = new();
        Grid grid2 = new() { ColumnSpacing = gridSplitter ? 16 : 0 };

        grid2.ColumnDefinitions.Add(new() { Width = left.Length, MinWidth = left.MinWidth });
        grid2.ColumnDefinitions.Add(new() { Width = right.Length, MinWidth = right.MinWidth });

        GridSplitter splitH = new()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 0, -16, 0),
            Opacity = 0.5f,
            CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover,
            ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment
        };

        grid2.Children.Add(left.Content);
        grid2.Children.Add(right.Content);
        Grid.SetColumn((FrameworkElement)right.Content, 1);
        if (gridSplitter)
            grid2.Children.Add(splitH);
        else
            grid2.Children.Add(new AppBarSeparator() { HorizontalAlignment = HorizontalAlignment.Right, Margin = new(0, 0, -2, 0) });

        AppBarSeparator separator = new() { HorizontalAlignment = HorizontalAlignment.Left, Margin = new Thickness(-2, 0, 0, 0) };
        grid.Children.Add(grid2);
        if (seperator)
            grid.Children.Add(separator);

        return grid;
    }

    private Grid PairHorizontal(GridDataTemeplate left, GridDataTemeplate center, GridDataTemeplate right, bool gridSplitter = true)
    {
        Grid grid = new() { ColumnSpacing = 16 };
        grid.ColumnDefinitions.Add(new() { Width = left.Length, MinWidth = left.MinWidth });
        grid.ColumnDefinitions.Add(new() { Width = center.Length, MinWidth = center.MinWidth });
        grid.ColumnDefinitions.Add(new() { Width = right.Length, MinWidth = right.MinWidth });

        GridSplitter splitH = new()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new(0, 0, -16, 0),
            Opacity = 0.5f,
            CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover,
            ResizeBehavior = GridSplitter.GridResizeBehavior.CurrentAndNext,
        };
        GridSplitter splitH2 = new()
        {
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new(0, 0, -16, 0),
            Opacity = 0.5f,
            CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover,
            ResizeBehavior = GridSplitter.GridResizeBehavior.PreviousAndNext,
        };
        Grid.SetColumn(splitH2, 1);

        grid.Children.Add(left.Content);
        grid.Children.Add(center.Content);
        Grid.SetColumn((FrameworkElement)center.Content, 1);
        grid.Children.Add(right.Content);
        Grid.SetColumn((FrameworkElement)right.Content, 2);
        if (gridSplitter)
            grid.Children.Add(splitH);
        if (gridSplitter)
            grid.Children.Add(splitH2);

        return grid;
    }

    private Grid WrapInTabView(Grid root, params TabViewItemDataTemplate[] i)
    {
        root.Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;

        TabViewPage tabViewPage = new(i);
        root.Children.Add(tabViewPage.TabView);

        return root;
    }

    private Grid WrapSplitView(Grid content, Grid pane)
    {
        Grid grid = new();
        SplitView = new() { OpenPaneLength = 333, IsPaneOpen = true, DisplayMode = SplitViewDisplayMode.Inline, PanePlacement = SplitViewPanePlacement.Right, Pane = pane, Content = content };

        BindingOperations.SetBinding(SplitView, SplitView.IsPaneOpenProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new("IsChecked") });

        grid.Children.Add(SplitView);

        return grid;
    }

    private Grid WrapGrid(UIElement content, List<Grid> list = null)
    {

        Grid grid = new();
        grid.Children.Add(content);

        if (list is not null)
            list.Add(grid);

        return grid;
    }
}
