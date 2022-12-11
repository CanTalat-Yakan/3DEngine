using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;

namespace Editor.Controller
{
    internal class GridDataTemeplate
    {
        public GridLength Length = new(1, GridUnitType.Star);
        public double MinWidth = 1;
        public double MinHeight = 40;
        public UIElement Content;
    }

    internal partial class Layout
    {
        public Grid Content;
        public ModelView.ViewPort ViewPort;
        public ModelView.Hierarchy Hierarchy;
        public ModelView.Properties Properties;
        public Grid PropertiesRoot;
        public ModelView.Output Output;
        public ModelView.Files Files;
        public Grid TabsRoot;

        public Layout(Grid content, 
            ModelView.ViewPort viewPort, 
            ModelView.Hierarchy hierarchy, 
            ModelView.Properties properties, 
            ModelView.Output output, 
            ModelView.Files files)
        {
            Content = content;
            ViewPort = viewPort;
            Hierarchy = hierarchy;
            Properties = properties;
            Output = output;
            Files = files;

            PropertiesRoot = new();
            PropertiesRoot.Children.Add(Properties);

            TabsRoot = new();
        }

        public void CreateLayout()
        {
            var tabView = WrapInTabView(TabsRoot,
                new() { Content = Files, Header = "Files", Symbol = Symbol.Document },
                new() { Content = Output, Header = "Output", Symbol = Symbol.Message });

            var content = PairVertical(
                new() { Content = WrapGrid(ViewPort), MinHeight = 0 },
                new() { Content = tabView, MinHeight = 0, Length = new(235, GridUnitType.Pixel) });

            var pane = PairVertical(
                new() { Content = WrapGrid(Hierarchy), MinHeight = 0 },
                new() { Content = WrapGrid(PropertiesRoot), MinHeight = 0, Length = new(1, GridUnitType.Star) });

            Content.Children.Add(WrapSplitView(content, pane));
        }
    }

    internal partial class Layout
    {
        private Grid PairVertical(GridDataTemeplate top, GridDataTemeplate bottom)
        {
            RowDefinition bottomRowDefinition;
            Grid grid = new() { };
            grid.RowDefinitions.Add(new() { Height = top.Length, MinHeight = top.MinHeight });
            grid.RowDefinitions.Add(bottomRowDefinition = new() { Height = bottom.Length, MinHeight = bottom.MinHeight });

            GridSplitter splitV = new() { VerticalAlignment = VerticalAlignment.Top, CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover };

            ((Grid)bottom.Content).Margin = new(0, 16, 0, 0);
            ((Grid)top.Content).Padding = new(0, 16, 0, 0);
            grid.Padding = new(0, -16, 0, 0);

            grid.Children.Add(top.Content);
            grid.Children.Add(bottom.Content);
            Grid.SetRow((FrameworkElement)bottom.Content, 1);
            Grid.SetRow(splitV, 1);
            grid.Children.Add(splitV);

            BindingOperations.SetBinding(bottomRowDefinition, RowDefinition.HeightProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new("IsChecked"), Converter = new BooleanToRowHeightConverter(bottom.Length) });

            return grid;
        }

        private Grid PairHorizontal(GridDataTemeplate left, GridDataTemeplate right)
        {
            Grid grid = new() { ColumnSpacing = 16 };
            grid.ColumnDefinitions.Add(new() { Width = left.Length, MinWidth = left.MinWidth });
            grid.ColumnDefinitions.Add(new() { Width = right.Length, MinWidth = right.MinWidth });

            GridSplitter splitH = new()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, -16, 0),
                Opacity = 0.5f,
                CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover,
                ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment
            };

            grid.Children.Add(left.Content);
            grid.Children.Add(right.Content);
            Grid.SetColumn((FrameworkElement)right.Content, 1);
            grid.Children.Add(splitH);


            return grid;
        }

        private Grid PairHorizontal(GridDataTemeplate left, GridDataTemeplate center, GridDataTemeplate right)
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
            grid.Children.Add(splitH);
            grid.Children.Add(splitH2);


            return grid;
        }

        private Grid WrapInTabView(Grid root, params TabViewItemDataTemplate[] i)
        {
            root.Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;

            TabViewPage tabViewPage = new(i);
            root.Children.Add(tabViewPage.TabView);

            //BindingOperations.SetBinding(root, Grid.VisibilityProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new PropertyPath("IsChecked"), Converter = new BooleanToVisibilityConverter() });

            return root;
        }

        private Grid WrapSplitView(Grid content, Grid pane)
        {
            Grid grid = new();
            SplitView split = new() { OpenPaneLength = 333, IsPaneOpen = true, DisplayMode = SplitViewDisplayMode.Inline, PanePlacement = SplitViewPanePlacement.Right, Pane = pane, Content = content };

            BindingOperations.SetBinding(split, SplitView.IsPaneOpenProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new("IsChecked") });

            grid.Children.Add(split);

            return grid;
        }

        private Grid WrapGrid(UIElement content)
        {
            Grid grid = new();
            grid.Children.Add(content);

            return grid;
        }
    }
}