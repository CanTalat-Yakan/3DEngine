using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Data;
using System;
using Editor.UserControls;
using Microsoft.UI.Xaml.Media;

namespace Editor.Controls
{
    class GridDataTemeplate
    {
        public GridLength Length = new GridLength(1, GridUnitType.Star);
        public double MinWidth = 1;
        public double MinHeight = 40;
        public UIElement Content;
    }
    internal class CLayout
    {
        internal Grid m_GridContent;

        internal Grid m_Main;
        internal ViewPort m_ViewPort;
        internal Hierarchy m_Hierarchy;
        internal Properties m_Properties;
        internal Output m_Output;
        internal Files m_Files;
        internal Settings m_Settings;

        public CLayout(Grid _content, ViewPort _viewPort, Hierarchy _hierarchy, Properties _properties, Output _output, Files _files, Settings _settings)
        {
            m_Main = _content;
            m_ViewPort = _viewPort;
            m_Hierarchy = _hierarchy;
            m_Properties = _properties;
            m_Output = _output;
            m_Files = _files;
            m_Settings = _settings;
        }

        internal void Initialize()
        {
            m_GridContent = CreateLayout(
                WrapGrid(m_ViewPort),
                WrapInTabView(
                    new TabViewItemDataTemplate() { Header = "Files", Content = m_Files, Symbol = Symbol.Document },
                    new TabViewItemDataTemplate() { Header = "Output", Content = m_Output, Symbol = Symbol.Message }),
                WrapGrid(m_Hierarchy),
                WrapGrid(m_Properties));

            m_Main.Children.Add(m_GridContent);
        }

        Grid CreateLayout(params Grid[] _panel)
        {
            var a = PairVertical(
                new GridDataTemeplate() { Content = _panel[0], MinHeight = 0 },
                new GridDataTemeplate() { Content = _panel[1], MinHeight = 0, Length = new GridLength(250, GridUnitType.Pixel) });

            var b = PairVertical(
                new GridDataTemeplate() { Content = _panel[2], MinHeight = 0 },
                new GridDataTemeplate() { Content = _panel[3], MinHeight = 0, Length = new GridLength(1.5f, GridUnitType.Star) });

            return WrapSplitView(a, b);
        }

        Grid PairHorizontal(GridDataTemeplate _left, GridDataTemeplate _right)
        {
            Grid grid = new Grid() { ColumnSpacing = 16 };
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = _left.Length, MinWidth = _left.MinWidth });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = _right.Length, MinWidth = _right.MinWidth });

            GridSplitter splitH = new GridSplitter()
            {
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(0, 0, -16, 0),
                Opacity = 0.5f,
                CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover,
                ResizeBehavior = GridSplitter.GridResizeBehavior.BasedOnAlignment
            };

            grid.Children.Add(_left.Content);
            grid.Children.Add(_right.Content);
            Grid.SetColumn((FrameworkElement)_right.Content, 1);
            grid.Children.Add(splitH);


            return grid;
        }
        Grid PairHorizontal(GridDataTemeplate _left, GridDataTemeplate _center, GridDataTemeplate _right)
        {
            Grid grid = new Grid() { ColumnSpacing = 16 };
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = _left.Length, MinWidth = _left.MinWidth });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = _center.Length, MinWidth = _center.MinWidth });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = _right.Length, MinWidth = _right.MinWidth });

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

            grid.Children.Add(_left.Content);
            grid.Children.Add(_center.Content);
            Grid.SetColumn((FrameworkElement)_center.Content, 1);
            grid.Children.Add(_right.Content);
            Grid.SetColumn((FrameworkElement)_right.Content, 2);
            grid.Children.Add(splitH);
            grid.Children.Add(splitH2);


            return grid;
        }
        Grid PairVertical(GridDataTemeplate _top, GridDataTemeplate _bottom)
        {
            Grid grid = new Grid() { };
            grid.RowDefinitions.Add(new RowDefinition() { Height = _top.Length, MinHeight = _top.MinHeight });
            grid.RowDefinitions.Add(new RowDefinition() { Height = _bottom.Length, MinHeight = _bottom.MinHeight });

            GridSplitter splitV = new GridSplitter() { VerticalAlignment = VerticalAlignment.Top, Opacity = 0.5f, CursorBehavior = GridSplitter.SplitterCursorBehavior.ChangeOnGripperHover };

            ((Grid)_bottom.Content).Margin = new Thickness(0, 16, 0, 0);
            ((Grid)_top.Content).Padding = new Thickness(0, 16, 0, 0);
            grid.Padding = new Thickness(0, -16, 0, 0);

            grid.Children.Add(_top.Content);
            grid.Children.Add(_bottom.Content);
            Grid.SetRow((FrameworkElement)_bottom.Content, 1);
            Grid.SetRow(splitV, 1);
            grid.Children.Add(splitV);

            return grid;
        }

        Grid WrapInTabView(params TabViewItemDataTemplate[] _i)
        {
            Grid grid = new Grid();
            CTabViewPage tabViewPage = new CTabViewPage(_i);
            grid.Children.Add(tabViewPage.m_TabView);

            //BindingOperations.SetBinding(grid, Grid.VisibilityProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new PropertyPath("IsChecked"), Converter = new BooleanToVisibilityConverter() });

            return grid;
        }
        Grid WrapSplitView(Grid _content, Grid _pane)
        {
            Grid grid = new Grid();
            SplitView split = new SplitView() { OpenPaneLength = 333, IsPaneOpen = true, DisplayMode = SplitViewDisplayMode.Inline, PanePlacement = SplitViewPanePlacement.Right, Pane = _pane, Content = _content };
            //BindingOperations.SetBinding(split, SplitView.IsPaneOpenProperty, new Binding() { ElementName = "x_AppBarToggleButton_Status_OpenPane", Path = new PropertyPath("IsChecked") });
            grid.Children.Add(split);

            return grid;
        }
        Grid WrapGrid(UIElement _content)
        {
            Grid grid = new Grid();
            grid.Children.Add(_content);

            return grid;
        }

    }
}
public sealed class BooleanToVisibilityConverter : IValueConverter
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
