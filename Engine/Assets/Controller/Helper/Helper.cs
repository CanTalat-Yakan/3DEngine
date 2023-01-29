using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Linq;
using System.Numerics;
using System.Text;
using System;
using Windows.Foundation;
using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using Expander = Microsoft.UI.Xaml.Controls.Expander;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using Rectangle = Microsoft.UI.Xaml.Shapes.Rectangle;
using FontFamily = Microsoft.UI.Xaml.Media.FontFamily;
using Image = Microsoft.UI.Xaml.Controls.Image;

namespace Editor.Controller
{
    internal class Helper
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

            ToggleButton button = new() { IsChecked = isChecked, Padding = new(7) };
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
            NavigationViewItemSeparator seperator = new() { Margin = new(10) };

            return seperator;
        }

        internal virtual UIElement CreateText(string placeholder = "Example")
        {
            TextBlock textInput = new() { Text = placeholder, MaxWidth = 200, TextWrapping = TextWrapping.Wrap };

            return textInput;
        }

        internal virtual UIElement CreateTextEqual(string placeholder = "Example")
        {
            TextBlock textInput = new() { Text = placeholder, MaxWidth = 200 };

            return textInput;
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

        internal virtual Grid CreateNumberInput(float f = 0, float min = float.MinValue, float max = float.MaxValue,
            TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs> onValueChanged = null)
        {
            NumberBox numInput = new() { Value = f, Minimum = min, Maximum = max, SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline, MaxWidth = 200 };
            numInput.ValueChanged += onValueChanged;

            return StackInGrid(numInput);
        }

        internal virtual UIElement CreateSlider(double f = 0, double min = 0, double max = 100,
            RangeBaseValueChangedEventHandler onValueChanged = null)
        {
            Slider numInput = new() { Value = f, Minimum = min, Maximum = max, Width = 200, Margin = new(0, 0, 0, -5.5) };
            if (onValueChanged != null)
                numInput.ValueChanged += onValueChanged;

            TextBlock numPreview = new() { Padding = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

            return StackInGrid(numInput, numPreview);
        }

        internal virtual Grid CreateTextInput(string placeholder = "Example")
        {
            TextBox textInput = new() { Text = placeholder, MaxWidth = 200 };

            return StackInGrid(textInput);
        }

        internal virtual Grid CreateVec2Input(Vector2 v = new())
        {
            NumberBox numInput = new() { Value = v.X, Margin = new(0, 0, 4, 0), MaxWidth = 98 };
            NumberBox num2Input = new() { Value = v.Y, Margin = new(0, 0, 4, 0), MaxWidth = 98 };

            return StackInGrid(numInput, num2Input);
        }

        internal virtual Grid CreateVec3Input(Vector3 v = new())
        {
            NumberBox numInput = new() { Value = v.X, Margin = new(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num2Input = new() { Value = v.Y, Margin = new(0, 0, 4, 0), MaxWidth = 64 };
            NumberBox num3Input = new() { Value = v.Z, Margin = new(0, 0, 4, 0), MaxWidth = 64 };

            return StackInGrid(numInput, num2Input, num3Input);
        }

        internal virtual Grid CreateVec3InputTransform(Vector3 v = new())
        {
            Rectangle rectangleR = new() { Fill = new SolidColorBrush(Colors.IndianRed), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox numInput = new() { Value = Math.Round(v.X, 4), Margin = new(0, 0, 4, 0), Width = 64 };

            Rectangle rectangleG = new() { Fill = new SolidColorBrush(Colors.SeaGreen), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox num2Input = new() { Value = Math.Round(v.Y, 4), Margin = new(0, 0, 4, 0), MaxWidth = 64 };

            Rectangle rectangleB = new() { Fill = new SolidColorBrush(Colors.DodgerBlue), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox num3Input = new() { Value = Math.Round(v.Z, 4), Margin = new(0, 0, 4, 0), MaxWidth = 64 };

            return StackInGrid(rectangleR, numInput, rectangleG, num2Input, rectangleB, num3Input);
        }

        internal virtual Grid CreateVec3InputTransform(Vector3 v = new(),
            params TypedEventHandler<NumberBox, NumberBoxValueChangedEventArgs>[] e)
        {
            Rectangle rectangleR = new() { Fill = new SolidColorBrush(Colors.IndianRed), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox numInput = new() { Value = Math.Round(v.X, 4), Margin = new(0, 0, 4, 0), Width = 64 };
            if (e[0] != null)
                numInput.ValueChanged += e[0];

            Rectangle rectangleG = new() { Fill = new SolidColorBrush(Colors.SeaGreen), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox num2Input = new() { Value = Math.Round(v.Y, 4), Margin = new(0, 0, 4, 0), MaxWidth = 64 };
            if (e[1] != null)
                num2Input.ValueChanged += e[1];

            Rectangle rectangleB = new() { Fill = new SolidColorBrush(Colors.DodgerBlue), RadiusX = 2, RadiusY = 2, Width = 4 };
            NumberBox num3Input = new() { Value = Math.Round(v.Z, 4), Margin = new(0, 0, 4, 0), MaxWidth = 64 };
            if (e[2] != null)
                num3Input.ValueChanged += e[2];

            return StackInGrid(rectangleR, numInput, rectangleG, num2Input, rectangleB, num3Input);
        }

        internal virtual Grid CreateBool(bool b = false)
        {
            CheckBox check = new() { IsChecked = b, Margin = new(0, 0, 0, -5.5) };

            return StackInGrid(check);
        }

        internal virtual Grid CreateEnum(params string[] items)
        {
            ComboBox comboBox = new() { FontSize = 13.5f, HorizontalAlignment = HorizontalAlignment.Stretch };

            foreach (var item in items)
                comboBox.Items.Add(item);

            comboBox.SelectedIndex = 0;

            return StackInGrid(comboBox);
        }

        internal virtual Grid CreateEvent(string s = "Event",
            RoutedEventHandler e = null)
        {
            Button button = new() { Content = s };
            button.Click += e;

            return StackInGrid(button);
        }

        internal virtual Grid CreateColorButton(byte r = 0, byte g = 0, byte b = 0, byte a = 0)
        {
            Windows.UI.Color col = new();
            col.R = r; col.G = g; col.B = b; col.A = a;
            ColorPickerButton colbutton = new() { SelectedColor = col };

            Style stylee = new() { TargetType = typeof(ColorPicker) };
            stylee.Setters.Add(new Setter(ColorPicker.ColorSpectrumShapeProperty, ColorSpectrumShape.Ring));
            stylee.Setters.Add(new Setter(ColorPicker.IsAlphaEnabledProperty, true));
            stylee.Setters.Add(new Setter(ColorPicker.IsHexInputVisibleProperty, true));
            colbutton.ColorPickerStyle = stylee;

            return StackInGrid(colbutton);
        }

        internal virtual Grid CreateHeader(string s = "Header")
        {
            Grid grid = new();
            TextBlock header = new() { Text = s, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new(0, 20, 0, 0) };

            grid.Children.Add(header);

            return grid;
        }

        internal virtual Grid CreateSpacer()
        {
            Grid grid = new() { Height = 10 };

            return grid;
        }

        internal virtual Grid CreateButton(string s,
            TappedEventHandler tapped)
        {
            Grid grid = new();

            Button button = new() { Content = s, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(10) };
            button.Tapped += tapped;

            grid.Children.Add(button);

            return grid;
        }

        internal virtual Grid CreateTreeView(out TreeView tree,
            DataTemplateSelector templateSelector = null)
        {
            Grid grid = new();

            tree = new() { ItemTemplateSelector = templateSelector, SelectionMode = TreeViewSelectionMode.Single, HorizontalAlignment = HorizontalAlignment.Stretch };

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

        internal virtual Grid CreateTextureSlot(string s = "None", string type = "type")
        {
            Grid container = new() { Width = 48, Height = 48 };
            Image img = new() { Stretch = Stretch.UniformToFill };
            Button button = new() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
            TextBlock path = new() { Text = s + $" ({type})", TextWrapping = TextWrapping.WrapWholeWords, MaxWidth = 200, Margin = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            container.Children.Add(img);
            container.Children.Add(button);

            return StackInGrid(container, path);
        }

        internal virtual Grid CreateReferenceSlot(string s = "None", string type = "type")
        {
            Button button = new() { Content = "..." };
            TextBlock reference = new() { Text = s + $" ({type})", TextWrapping = TextWrapping.WrapWholeWords, MaxWidth = 200, Margin = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

            return StackInGrid(button, reference);
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
        public static float Remap(this float value, float sourceMin, float sourceMax, float targetMin, float targetMax)
        {
            return (value - sourceMin) / (sourceMax - sourceMin) * (targetMax - targetMin) + targetMin;
        }

        public static Grid WrapInGrid(this UIElement content)
        {
            Grid grid = new();
            grid.Children.Add(content);

            return grid;
        }

        public static Grid StackInGrid(this UIElement[] content, float spacing = 5)
        {
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            StackPanel stack = new() { Spacing = spacing, Orientation = Orientation.Vertical, FlowDirection = FlowDirection.LeftToRight };

            foreach (var item in content)
                stack.Children.Add(item);

            grid.Children.Add(stack);

            return grid;
        }

        public static Grid StackInGrid(this Grid[] content, float spacing = 5)
        {
            Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
            StackPanel stack = new() { Spacing = spacing, Orientation = Orientation.Vertical, FlowDirection = FlowDirection.LeftToRight };

            foreach (var item in content)
                stack.Children.Add(item);

            grid.Children.Add(stack);

            return grid;
        }

        public static Grid WrapInField(this UIElement content, string text)
        {
            Grid grid = new();
            StackPanel stack = new() { Orientation = Orientation.Horizontal };
            TextBlock header = new() { Text = text.FormatString(), Width = 80, TextWrapping = TextWrapping.WrapWholeWords, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            stack.Children.Add(content);

            grid.Children.Add(stack);

            return grid;
        }

        public static Grid WrapInFieldEqual(this UIElement content, string text)
        {
            Grid grid = new();
            StackPanel stack = new() { Orientation = Orientation.Horizontal };
            TextBlock header = new() { Text = text.FormatString(), Width = 160, TextWrapping = TextWrapping.WrapWholeWords, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            stack.Children.Add(content);
            grid.Children.Add(stack);

            return grid;
        }

        public static Grid WrapInGridVertical(this UIElement content, string text)
        {
            Grid grid = new();
            StackPanel stack = new() { Orientation = Orientation.Vertical, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Stretch };
            TextBlock header = new() { Text = text, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Bottom };

            stack.Children.Add(header);
            stack.Children.Add(content);

            grid.Children.Add(stack);

            return grid;
        }

        public static Grid WrapInExpander(this Grid content, string text)
        {
            Grid grid = new() { Margin = new(0, 0, 0, 2) };
            Expander expander = new()
            {
                Header = text,
                Padding = new(15),
                ExpandDirection = ExpandDirection.Down,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            expander.IsExpanded = true;
            expander.Content = content;

            grid.Children.Add(expander);

            return grid;
        }

        public static Grid WrapInExpanderWithToggleButton(this Grid content, string text, RoutedEventHandler onClick = null)
        {
            Grid grid = new() { Margin = new(0, 0, 0, 2) };
            Expander expander = new()
            {
                Padding = new(15),
                ExpandDirection = ExpandDirection.Down,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            ToggleButton toggleButton = new() { Content = text, IsChecked = true };
            if (onClick != null)
                toggleButton.Click += onClick;

            expander.Header = toggleButton;
            expander.Content = content;

            grid.Children.Add(expander);

            return grid;
        }

        public static Grid WrapInExpanderWithEditableHeader(this Grid content, string text)
        {
            Grid grid = new() { Margin = new(0, 0, 0, 2) };
            Expander expander = new()
            {
                Padding = new(15),
                ExpandDirection = ExpandDirection.Down,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Stretch
            };
            expander.Header = new TextBox() { Text = text, Margin = new(0) };
            expander.Content = content;

            grid.Children.Add(expander);

            return grid;
        }

        public static UIElement AddContentFlyout(this UIElement content, FlyoutBase flyout)
        {
            content.ContextFlyout = flyout;

            return content;
        }

        public static async void CreateDialogAsync(this ContentDialog contentDialog) => await contentDialog.ShowAsync();

        public static string AddSpacesToSentence(this string text, bool preserveAcronyms = true)
        {
            if (string.IsNullOrWhiteSpace(text))
                return string.Empty;
            StringBuilder newText = new StringBuilder(text.Length * 2);
            newText.Append(text[0]);
            for (int i = 1; i < text.Length; i++)
            {
                if (char.IsUpper(text[i]) || char.IsDigit(text[i]))
                    if (!char.IsDigit(text[i - 1]))
                        if ((text[i - 1] != ' ' && !char.IsUpper(text[i - 1])) ||
                        (preserveAcronyms && char.IsUpper(text[i - 1]) &&
                         i < text.Length - 1 && !char.IsUpper(text[i + 1])))
                            newText.Append(' ');
                newText.Append(text[i]);
            }

            return newText.ToString();
        }

        public static string SplitLast(this string text, char seperator)
        {
            return text.Split(seperator).Last();
        }

        public static string FirstCharToUpper(this string input)
        {
            return string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));
        }

        public static string FormatString(this string text)
        {
            return text.SplitLast('_').SplitLast('.').FirstCharToUpper().AddSpacesToSentence();
        }
    }

    internal sealed class BooleanToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool bValue = false;
            if (value is bool)
                bValue = (bool)value;
            else if (value is bool?)
            {
                bool? tmp = (bool?)value;
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

    internal sealed class BooleanToRowHeightConverter : IValueConverter
    {
        GridLength _initialValue;

        public BooleanToRowHeightConverter(GridLength initialValue)
        {
            _initialValue = initialValue;
        }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            bool bValue = false;
            if (value is bool)
                bValue = (bool)value;
            else if (value is bool?)
            {
                bool? tmp = (bool?)value;
                bValue = tmp.HasValue ? tmp.Value : false;
            }
            return (bValue) ? _initialValue : new(0);
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            if (value is GridLength)
                return (GridLength)value == _initialValue;
            else
                return false;
        }
    }
}
