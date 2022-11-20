using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using Editor.UserControls;
using Microsoft.UI.Xaml.Media;

namespace Editor.Controls
{
    internal class GridDataTemeplate
    {
        public GridLength Length = new GridLength(1, GridUnitType.Star);
        public double MinWidth = 1;
        public double MinHeight = 40;
        public UIElement Content;
    }

    internal class LayoutController
    {
        public Grid Content;
        public ViewPort ViewPort;
        public Hierarchy Hierarchy;
        public Properties Properties;
        public Grid PropertiesRoot;
        public Output Output;
        public Files Files;
        public Grid TabsRoot;

        public LayoutController(Grid content, ViewPort viewPort, Hierarchy hierarchy, Properties properties, Output output, Files files)
        {
            Content = content;
            ViewPort = viewPort;
            Hierarchy = hierarchy;
            Properties = properties;
            Output = output;
            Files = files;

            PropertiesRoot = new Grid();
            PropertiesRoot.Children.Add(Properties);

            TabsRoot = new Grid();
        }

        public void Initialize()
        {
            Grid grid = CreateLayout(
                WrapGrid(ViewPort),
                WrapInTabView(TabsRoot,
                    new TabViewItemDataTemplate() { Header = "Files⠀⠀⠀⠀", Content = Files, Symbol = Symbol.Document },
                    new TabViewItemDataTemplate() { Header = "Output⠀⠀⠀⠀", Content = Output, Symbol = Symbol.Message }),
                WrapGrid(Hierarchy),
                WrapGrid(PropertiesRoot));

            Content.Children.Add(grid);
        }

        private Grid CreateLayout(params Grid[] panel)
        {
            var a = PairVertical(
                new GridDataTemeplate() { Content = panel[0], MinHeight = 0 },
                new GridDataTemeplate() { Content = panel[1], MinHeight = 0, Length = new GridLength(225, GridUnitType.Pixel) });

            var b = PairVertical(
                new GridDataTemeplate() { Content = panel[2], MinHeight = 0 },
                new GridDataTemeplate() { Content = panel[3], MinHeight = 0, Length = new GridLength(1, GridUnitType.Star) });

            return WrapSplitView(a, b);
        }

        private Grid PairHorizontal(GridDataTemeplate left, GridDataTemeplate right)
        {
            Grid grid = new Grid() { ColumnSpacing = 16 };
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = left.Length, MinWidth = left.MinWidth });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = right.Length, MinWidth = right.MinWidth });

            GridSplitter splitH = new GridSplitter()
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
            Grid grid = new Grid() { ColumnSpacing = 16 };
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = left.Length, MinWidth = left.MinWidth });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = center.Length, MinWidth = center.MinWidth });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = right.Length, MinWidth = right.MinWidth });

            GridSplitter splitH = new GridSplitter()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, -16, 0),
                Opacity = 0.5f,
                CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover,
                ResizeBehavior = GridSplitter.GridResizeBehavior.CurrentAndNext,
            };
            GridSplitter splitH2 = new GridSplitter()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, -16, 0),
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

        private Grid PairVertical(GridDataTemeplate top, GridDataTemeplate bottom)
        {
            Grid grid = new Grid() { };
            grid.RowDefinitions.Add(new RowDefinition() { Height = top.Length, MinHeight = top.MinHeight });
            grid.RowDefinitions.Add(new RowDefinition() { Height = bottom.Length, MinHeight = bottom.MinHeight });

            GridSplitter splitV = new GridSplitter() { VerticalAlignment = VerticalAlignment.Top, Opacity = 0.5f, CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover };

            ((Grid)bottom.Content).Margin = new Thickness(0, 16, 0, 0);
            ((Grid)top.Content).Padding = new Thickness(0, 16, 0, 0);
            grid.Padding = new Thickness(0, -16, 0, 0);

            grid.Children.Add(top.Content);
            grid.Children.Add(bottom.Content);
            Grid.SetRow((FrameworkElement)bottom.Content, 1);
            Grid.SetRow(splitV, 1);
            grid.Children.Add(splitV);

            return grid;
        }

        private Grid WrapInTabView(Grid root, params TabViewItemDataTemplate[] i)
        {
            root.Background = Application.Current.Resources["ApplicationPageBackgroundThemeBrush"] as SolidColorBrush;

            TabViewPageController tabViewPage = new TabViewPageController(i);
            root.Children.Add(tabViewPage.Tab);

            //BindingOperations.SetBinding(grid, Grid.VisibilityProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new PropertyPath("IsChecked"), Converter = new BooleanToVisibilityConverter() });

            return root;
        }

        private Grid WrapSplitView(Grid content, Grid pane)
        {
            Grid grid = new Grid();
            SplitView split = new SplitView() { OpenPaneLength = 333, IsPaneOpen = true, DisplayMode = SplitViewDisplayMode.Inline, PanePlacement = SplitViewPanePlacement.Right, Pane = pane, Content = content };
            //BindingOperations.SetBinding(split, SplitView.IsPaneOpenProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new PropertyPath("IsChecked") });
            grid.Children.Add(split);

            return grid;
        }

        private Grid WrapGrid(UIElement content)
        {
            Grid grid = new Grid();
            grid.Children.Add(content);

            return grid;
        }
    }

    internal sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool bValue = false;
            if (value is bool)
                bValue = (bool)value;
            else if (value is Nullable<bool>)
            {
                Nullable<bool> tmp = (Nullable<bool>)value;
                bValue = tmp.HasValue ? tmp.Value : false;
            }
            return (bValue) ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is Visibility)
                return (Visibility)value == Visibility.Visible;
            else
                return false;
        }
    }
}