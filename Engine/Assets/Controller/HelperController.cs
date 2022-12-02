using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using Windows.Foundation;
using System;
using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using Expander = Microsoft.UI.Xaml.Controls.Expander;

namespace Editor.Controller
{
    internal class HelperController
    {
        internal virtual Grid StackInGrid(params UIElement[] content)
        {
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            StackPanel stack = new() { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };

            foreach (var item in content)
                stack.Children.Add(item);

            grid.Children.Add(stack);

            return grid;
        }

        internal virtual Grid StackInGridVertical(params UIElement[] content)
        {
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            StackPanel stack = new() { Orientation = Orientation.Vertical, FlowDirection = FlowDirection.LeftToRight };

            foreach (var item in content)
                stack.Children.Add(item);

            grid.Children.Add(stack);

            return grid;
        }
        internal virtual ComboBox CreateComboBox(params string[] items)
        {
            ComboBox comboBox = new() { Height = 33, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };

            foreach (var item in items)
                comboBox.Items.Add(item);

            return comboBox;
        }

        internal virtual UIElement CreateToggleButton(Grid icon, bool isChecked = false)
        {
            Viewbox viewbox = new() { Width = 16, Height = 16 };
            viewbox.Child = icon;

            ToggleButton button = new() { IsChecked = isChecked, Padding = new Thickness(7) };
            button.Content = viewbox;

            return button;
        }

        internal virtual UIElement CreateButton(Grid icon, Grid content)
        {
            Button button = new() { Content = icon };
            button.Flyout = new Flyout() { Content = content, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };

            return button;
        }

        internal virtual UIElement CreateButtonWithValue(Grid icon, float f, Grid content)
        {
            StackPanel stack = new() { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight, Spacing = 5 };
            Viewbox viewbox = new() { Width = 16, Height = 16 };
            viewbox.Child = icon;

            stack.Children.Add(viewbox);
            stack.Children.Add(new TextBlock() { Text = f.ToString() });

            Button button = new() { Content = stack, FlowDirection = FlowDirection.LeftToRight };
            button.Flyout = new Flyout() { Content = content, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };

            return button;
        }

        internal virtual UIElement CreateAppBarSeperator()
        {
            AppBarSeparator seperator = new();

            return seperator;
        }

        internal virtual UIElement CreateSeperator()
        {
            NavigationViewItemSeparator seperator = new NavigationViewItemSeparator() { Margin = new Thickness(10) };

            return seperator;
        }

        internal virtual Grid CreateText(string placeholder = "Example")
        {
            TextBlock textInput = new() { Text = placeholder, MaxWidth = 200, TextWrapping = TextWrapping.Wrap };

            return StackInGrid(textInput);
        }

        internal virtual Grid CreateTextEqual(string s = "String", string placeholder = "Example")
        {
            TextBlock textInput = new TextBlock() { Text = placeholder, MaxWidth = 200 };

            return textInput.WrapInFieldEqual(s);
        }

        internal virtual UIElement CreateTextFull(out TextBlock textBlock)
        {
            textBlock = new();

            return textBlock;
        }

        internal virtual UIElement CreateTextFull(string s = "String")
        {
            TextBlock textBlock = new() { Text = s, TextWrapping = TextWrapping.Wrap };

            return textBlock;
        }

        internal virtual UIElement CreateTextFullWithOpacity(string s = "String")
        {
            TextBlock textBlock = new() { Text = s, Opacity = 0.5f, TextWrapping = TextWrapping.Wrap };

            return textBlock;
        }

        internal virtual Grid CreateNumberInput(float f = 0, float min = float.MinValue, float max = float.MaxValue, TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs> onValueChanged = null)
        {
            NumberBox numInput = new() { Value = f, Minimum = min, Maximum = max, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, MaxWidth = 200 };
            numInput.ValueChanged += onValueChanged;

            return StackInGrid(numInput);
        }

        internal virtual UIElement CreateSlider(float f = 0, float min = float.MinValue, float max = float.MaxValue, RangeBaseValueChangedEventHandler onValueChanged = null)
        {
            Slider numInput = new() { Value = f, Minimum = min, Maximum = max, Width = 200, Margin = new Thickness(0, 0, 0, -5.5) };
            numInput.ValueChanged += onValueChanged;

            TextBlock numPreview = new() { Padding = new Thickness(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            return StackInGrid(numInput, numPreview);
        }

        internal virtual Grid CreateTextInput(string placeholder = "Example")
        {
            TextBox textInput = new() { Text = placeholder, MaxWidth = 200 };

            return StackInGrid(textInput);
        }

        internal virtual Grid CreateVec2Input(float x = 0, float y = 0)
        {
            NumberBox numInput = new() { Value = x, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 98 };
            NumberBox num2Input = new() { Value = y, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 98 };

            return StackInGrid(numInput, num2Input);
        }

        internal virtual Grid CreateVec3Input(float x = 0, float y = 0, float z = 0)
        {
            NumberBox numInput = new() { Value = x, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num2Input = new() { Value = y, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num3Input = new() { Value = z, Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            return StackInGrid(numInput, num2Input, num3Input);
        }

        internal virtual Grid CreateVec3InputTransform(float x = 0, float y = 0, float z = 0)
        {
            Rectangle rectangleR = new Rectangle() { Fill = new SolidColorBrush(Colors.IndianRed), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox numInput = new NumberBox() { Value = MathF.Round(x, 4), Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            Rectangle rectangleG = new Rectangle() { Fill = new SolidColorBrush(Colors.SeaGreen), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox num2Input = new NumberBox() { Value = MathF.Round(y, 4), Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            Rectangle rectangleB = new Rectangle() { Fill = new SolidColorBrush(Colors.DodgerBlue), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox num3Input = new NumberBox() { Value = MathF.Round(z, 4), Margin = new Thickness(0, 0, 4, 0), MaxWidth = 64 };

            return StackInGrid(rectangleR, numInput, rectangleG, num2Input, rectangleB, num3Input);
        }

        internal virtual Grid CreateBool(bool b = false)
        {
            CheckBox check = new() { IsChecked = b, Margin = new Thickness(0, 0, 0, -5.5) };

            return StackInGrid(check);
        }

        internal virtual Grid CreateEnum(params string[] items)
        {
            ComboBox comboBox = new ComboBox() { FontSize = 13.5f, HorizontalAlignment = HorizontalAlignment.Stretch };

            foreach (var item in items)
                comboBox.Items.Add(item);

            comboBox.SelectedIndex = 0;

            return StackInGrid(comboBox);
        }

        internal virtual Grid CreateEvent(string s2 = "Event", RoutedEventHandler e = null)
        {
            Button button = new Button() { Content = s2 };
            button.Click += e;

            return StackInGrid(button);
        }

        internal virtual Grid CreateColorButton(byte r = 0, byte g = 0, byte b = 0, byte a = 0)
        {
            Windows.UI.Color col = new();
            col.R = r; col.G = g; col.B = b; col.A = a;
            ColorPickerButton colbutton = new() { SelectedColor = col };

            var stylee = new Style { TargetType = typeof(ColorPicker) };
            stylee.Setters.Add(new Setter(ColorPicker.ColorSpectrumShapeProperty, ColorSpectrumShape.Ring));
            stylee.Setters.Add(new Setter(ColorPicker.IsAlphaEnabledProperty, true));
            stylee.Setters.Add(new Setter(ColorPicker.IsHexInputVisibleProperty, true));
            colbutton.ColorPickerStyle = stylee;

            return StackInGrid(colbutton);
        }

        internal virtual Grid CreateHeader(string s = "Header")
        {
            Grid grid = new();
            TextBlock header = new() { Text = s, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new Thickness(0, 20, 0, 0) };

            grid.Children.Add(header);

            return grid;
        }

        internal virtual Grid CreateSpacer()
        {
            Grid grid = new() { Height = 10 };

            return grid;
        }

        internal virtual Grid CreateButton(string s, TappedEventHandler tapped)
        {
            Grid grid = new();

            Button button = new() { Content = s, HorizontalAlignment = HorizontalAlignment.Center, Margin = new Thickness(10) };

            button.Tapped += tapped;

            AutoSuggestBox suggestBox = new();

            //FlyoutBase kbase = new FlyoutBase();
            //button.Flyout = suggestBox;

            grid.Children.Add(button);

            return grid;
        }

        internal virtual Grid CreateTreeView(out TreeView tree)
        {
            Grid grid = new Grid();

            tree = new TreeView() { SelectionMode = TreeViewSelectionMode.Single, HorizontalAlignment = HorizontalAlignment.Stretch };

            grid.Children.Add(tree);

            return grid;
        }

        internal virtual Grid CreateIcon(string glyph)
        {
            Grid grid = new();

            FontIcon icon = new() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = glyph };

            grid.Children.Add(icon);

            return grid;
        }

        internal virtual Grid CreateIcon(Symbol symbol)
        {
            Grid grid = new();

            SymbolIcon symbolIcon = new() { Symbol = symbol };

            grid.Children.Add(symbolIcon);

            return grid;
        }

        internal static string SizeSuffix(Int64 value, int decimalPlaces = 1)
        {
            string[] SizeSuffixes = { "bytes", "KB", "MB", "GB", "TB", "PB", "EB", "ZB", "YB" };

            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }

            int i = 0;
            decimal dValue = (decimal)value;
            while (Math.Round(dValue, decimalPlaces) >= 1000)
            {
                dValue /= 1024;
                i++;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}", dValue, SizeSuffixes[i]);
        }
    }

    internal static class ExtensionMethods
    {
        public static float Remap(this float _value, float _oldMin, float _oldMax, float _newMin, float _newMax)
        {
            return (_value - _oldMin) / (_oldMax - _oldMin) * (_newMax - _newMin) + _newMin;
        }

        public static Grid WrapInGrid(this UIElement content)
        {
            Grid grid = new();
            grid.Children.Add(content);

            return grid;
        }

        public static Grid StackInGrid(this UIElement[] content)
        {
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            StackPanel stack = new() { Spacing = 5, Orientation = Orientation.Vertical, FlowDirection = FlowDirection.LeftToRight };

            foreach (var item in content)
                stack.Children.Add(item);

            grid.Children.Add(stack);

            return grid;
        }

        public static Grid StackInGrid(this Grid[] content)
        {
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            StackPanel stack = new() { Spacing = 5, Orientation = Orientation.Vertical, FlowDirection = FlowDirection.LeftToRight };

            foreach (var item in content)
                stack.Children.Add(item);

            grid.Children.Add(stack);

            return grid;
        }

        public static Grid WrapInField(this UIElement content, string text)
        {
            Grid grid = new();
            StackPanel stack = new() { Orientation = Orientation.Horizontal };
            TextBlock header = new() { Text = text, Width = 80, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            stack.Children.Add(content);

            grid.Children.Add(stack);

            return grid;
        }

        public static Grid WrapInFieldEqual(this UIElement content, string text)
        {
            Grid grid = new Grid();
            StackPanel stack = new StackPanel() { Orientation = Orientation.Horizontal };
            TextBlock header = new TextBlock() { Text = text, Width = 160, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            stack.Children.Add(content);
            grid.Children.Add(stack);

            return grid;
        }

        public static Grid WrapInGridVertical(this UIElement content, string text)
        {
            Grid grid = new();
            StackPanel stack = new() { Orientation = Orientation.Vertical, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Stretch };
            TextBlock header = new() { Text = text, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            stack.Children.Add(content);

            grid.Children.Add(stack);

            return grid;
        }

        public static Grid WrapInExpander(this Grid content, string text)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            Expander expander = new Expander() { Header = text, ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            expander.IsExpanded = true;
            expander.Content = content;

            grid.Children.Add(expander);

            return grid;
        }

        public static Grid WrapInExpanderWithNoHeader(this Grid content)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            Expander expander = new Expander() { ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            expander.Header = new Grid() { Padding = new Thickness(0) };
            expander.IsExpanded = true;
            expander.Content = content;

            grid.Children.Add(expander);

            return grid;
        }

        public static Grid WrapInExpanderWithToggleButton(this Grid content, string text)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            Expander expander = new Expander() { ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            expander.Header = new ToggleButton() { Content = text, IsChecked = true };
            expander.Content = content;

            grid.Children.Add(expander);

            return grid;
        }

        public static Grid WrapInExpanderWithEditableHeader(this Grid content, string text)
        {
            Grid grid = new Grid() { Margin = new Thickness(0, 0, 0, 2) };
            Expander expander = new Expander() { ExpandDirection = ExpandDirection.Down, HorizontalAlignment = HorizontalAlignment.Stretch, HorizontalContentAlignment = HorizontalAlignment.Stretch };
            expander.Header = new TextBox() { Text = text, Margin = new Thickness(0) };
            expander.Content = content;

            grid.Children.Add(expander);

            return grid;
        }
        
        public static UIElement AddContentFlyout(this UIElement content, FlyoutBase flyout)
        {
            content.ContextFlyout= flyout;

            return content;
        }
    }
}
