using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System;
using Windows.ApplicationModel;

using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using Expander = Microsoft.UI.Xaml.Controls.Expander;
using FontFamily = Microsoft.UI.Xaml.Media.FontFamily;
using Image = Microsoft.UI.Xaml.Controls.Image;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;

namespace Editor.Controller;

internal class Helper
{
    internal static Grid StackInGrid(params UIElement[] content)
    {
        Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
        StackPanel stack = new() { Orientation = Orientation.Horizontal, FlowDirection = FlowDirection.LeftToRight };

        foreach (var item in content)
            stack.Children.Add(item);

        grid.Children.Add(stack);

        return grid;
    }

    internal static Grid StackInGridVertical(params UIElement[] content)
    {
        Grid grid = new() { HorizontalAlignment = HorizontalAlignment.Stretch };
        StackPanel stack = new() { Orientation = Orientation.Vertical, FlowDirection = FlowDirection.LeftToRight };

        stack.AddStackTransition();

        foreach (var item in content)
            stack.Children.Add(item);

        grid.Children.Add(stack);

        return grid;
    }

    internal static ComboBox CreateComboBox(params string[] items)
    {
        ComboBox comboBox = new() { Height = 33, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };

        foreach (var item in items)
            comboBox.Items.Add(item);

        return comboBox;
    }

    internal static UIElement CreateToggleButton(Grid icon, bool isChecked = false)
    {
        Viewbox viewbox = new() { Width = 16, Height = 16 };
        viewbox.Child = icon;

        ToggleButton button = new() { IsChecked = isChecked, Padding = new(7) };
        button.Content = viewbox;

        return button;
    }

    internal static UIElement CreateFlyoutButton(Grid icon, Grid content)
    {
        Button button = new() { Content = icon };
        button.Flyout = new Flyout() { Content = content, Placement = FlyoutPlacementMode.BottomEdgeAlignedRight };

        return button;
    }

    internal static UIElement CreateFlyoutButtonWithValue(Grid icon, float f, Grid content)
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

    internal static UIElement CreateAppBarSeperator() =>
        new AppBarSeparator();

    internal static UIElement CreateSeperator() =>
        new NavigationViewItemSeparator() { Margin = new(10) };

    internal static UIElement CreateText(string placeholder = "Example") =>
        new TextBlock() { Text = placeholder, MaxWidth = 200, TextWrapping = TextWrapping.Wrap };

    internal static UIElement CreateTextEqual(string placeholder = "Example") =>
        new TextBlock() { Text = placeholder, MaxWidth = 200 };

    internal static UIElement CreateTextFull(out TextBlock textBlock) =>
        textBlock = new();

    internal static UIElement CreateTextFull(string s = "String") =>
        new TextBlock() { Text = s, TextWrapping = TextWrapping.Wrap };

    internal static UIElement CreateTextFullWithOpacity(string s = "String") =>
        new TextBlock() { Text = s, Opacity = 0.5f, TextWrapping = TextWrapping.Wrap };


    #region // Properties View Field Bindings
    internal static Grid CreateTextInput(object source, string fieldName, string placeholder = "Example")
    {
        TextBox textBox = new() { Text = placeholder, MaxWidth = 200 };

        Binding.Get(fieldName + source)?.Set(textBox, "Text", "TextChanging");

        return StackInGrid(textBox);
    }

    internal static Grid CreateNumberInput(object source, string fieldName, float value = 0, float min = float.MinValue, float max = float.MaxValue)
    {
        NumberBox numberBox = new() { Value = value, Minimum = min, Maximum = max, MaxWidth = 200 };
        numberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline;

        Binding.Get(fieldName + source)?.Set(numberBox, "Value", "ValueChanged");

        return StackInGrid(numberBox);
    }

    internal static Grid CreateNumberInputInt(object source, string fieldName, float value = 0, float min = float.MinValue, float max = float.MaxValue)
    {
        NumberBox numberBox = new() { Value = value, Minimum = min, Maximum = max, MaxWidth = 200 };
        numberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Inline;

        Binding.Get(fieldName + source)?.Set(numberBox, "Value", "ValueChanged");

        return StackInGrid(numberBox);
    }

    internal static UIElement CreateSlider(object source, string fieldName, double value = 0, double min = 0, double max = 100)
    {
        Slider slider = new() { Value = value, Minimum = min, Maximum = max, Width = 180, Margin = new(0, 0, 0, -5.5) };
        TextBlock numberPreview = new() { Padding = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

        Binding.Get(fieldName + source)?.Set(slider, "Value", "ValueChanged");
        Binding.Get(fieldName + source)?.SetEvent((s, e) =>
            numberPreview.Text = Binding.Get(fieldName + source)?.Value.ToString());

        return StackInGrid(slider, numberPreview);
    }

    internal static Grid CreateVec2Input(object source, string fieldName, Vector2 value = new())
    {
        NumberBoxVector2 numberBoxVector2 = new() { Value = value };

        Binding.Get(fieldName + source)?.Set(numberBoxVector2, "Value", "ValueChanged");

        return numberBoxVector2.GetStackInGrid();
    }

    internal static Grid CreateVec2InputWithRG(object source, string fieldName, Vector2 value = new())
    {
        NumberBoxVector2 numberBoxVector2 = new() { Value = value };

        Binding.Get(fieldName + source)?.Set(numberBoxVector2, "Value", "ValueChanged");

        return numberBoxVector2.GetStackInGridWithRG();
    }

    internal static Grid CreateVec3Input(object source, string fieldName, Vector3 value = new())
    {
        NumberBoxVector3 numberBoxVector3 = new() { Value = value };

        Binding.Get(fieldName + source)?.Set(numberBoxVector3, "Value", "ValueChanged");

        return numberBoxVector3.GetStackInGrid();
    }

    internal static Grid CreateVec3InputWithRGB(object source, string fieldName, Vector3 value = new())
    {
        NumberBoxVector3 numberBoxVector3 = new() { Value = value };

        Binding.Get(fieldName + source)?.Set(numberBoxVector3, "Value", "ValueChanged");

        return numberBoxVector3.GetStackInGridWithRGB();
    }

    internal static Grid CreateBool(object source, string fieldName, bool value = false)
    {
        CheckBox checkBox = new() { IsChecked = value, Margin = new(0, 0, 0, -5.5) };

        Binding.Get(fieldName + source)?.Set(checkBox, "IsChecked", "Click");

        return StackInGrid(checkBox);
    }
    #endregion


    internal static Grid CreateEnum(params string[] items)
    {
        ComboBox comboBox = new() { FontSize = 13.5f, HorizontalAlignment = HorizontalAlignment.Stretch };

        foreach (var item in items)
            comboBox.Items.Add(item);

        comboBox.SelectedIndex = 0;

        return StackInGrid(comboBox);
    }

    internal static Grid CreateEvent(string s = "Event",
        RoutedEventHandler e = null)
    {
        Button button = new() { Content = s };
        button.Click += e;

        return StackInGrid(button);
    }

    internal static Grid CreateColorButton(byte r = 0, byte g = 0, byte b = 0, byte a = 0)
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

    internal static Grid CreateHeader(string s = "Header")
    {
        Grid grid = new();
        TextBlock header = new() { Text = s, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new(0, 20, 0, 0) };

        grid.Children.Add(header);

        return grid;
    }

    internal static Grid CreateSpacer() =>
        new Grid() { Height = 10 };

    internal static Grid CreateButton(string s,
        TappedEventHandler tapped)
    {
        Grid grid = new();

        Button button = new() { Content = s, HorizontalAlignment = HorizontalAlignment.Center, Margin = new(10) };
        button.Tapped += tapped;

        grid.Children.Add(button);

        return grid;
    }

    internal static Grid CreateTreeView(out TreeView tree,
        DataTemplateSelector templateSelector = null)
    {
        Grid grid = new();

        tree = new() { ItemTemplateSelector = templateSelector, SelectionMode = TreeViewSelectionMode.Single, HorizontalAlignment = HorizontalAlignment.Stretch };

        grid.Children.Add(tree);

        return grid;
    }

    internal static Grid CreateIcon(string glyph)
    {
        Grid grid = new();

        FontIcon icon = new() { FontFamily = new FontFamily("Segoe MDL2 Assets"), Glyph = glyph };

        grid.Children.Add(icon);

        return grid;
    }

    internal static Grid CreateIcon(Symbol symbol)
    {
        Grid grid = new();

        SymbolIcon symbolIcon = new() { Symbol = symbol };

        grid.Children.Add(symbolIcon);

        return grid;
    }

    internal static Grid CreateTextureSlot(string s = "None", string type = "type")
    {
        Grid container = new() { Width = 48, Height = 48 };
        Image img = new() { Stretch = Stretch.UniformToFill };
        Button button = new() { HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch };
        TextBlock path = new() { Text = s + $" ({type})", TextWrapping = TextWrapping.WrapWholeWords, MaxWidth = 200, Margin = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

        container.Children.Add(img);
        container.Children.Add(button);

        return StackInGrid(container, path);
    }

    internal static Grid CreateReferenceSlot(string s = "None", string type = "type")
    {
        Button button = new() { Content = "..." };
        TextBlock reference = new() { Text = s + $" ({type})", TextWrapping = TextWrapping.WrapWholeWords, MaxWidth = 200, Margin = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Bottom };

        return StackInGrid(button, reference);
    }

    internal static string GetAppVersion()
    {
        Package package = Package.Current;
        PackageId packageId = package.Id;
        PackageVersion version = packageId.Version;

        return "Version " + string.Format("{0}.{1}.{2}.{3}", version.Major, version.Minor, version.Build, version.Revision);

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
    public static float Remap(this float value, float sourceMin, float sourceMax, float targetMin, float targetMax) =>
        (value - sourceMin) / (sourceMax - sourceMin) * (targetMax - targetMin) + targetMin;

    public static UIElement AddToolTip(this UIElement content, ToolTip toolTip)
    {

        if (toolTip is not null)
            if (!string.IsNullOrEmpty((string)toolTip.Content))
                ToolTipService.SetToolTip(content, toolTip);

        return content;
    }

    public static Grid AddToolTip(this Grid content, ToolTip toolTip)
    {
        if (toolTip is not null)
            if (!string.IsNullOrEmpty((string)toolTip.Content))
                ToolTipService.SetToolTip(content, toolTip);

        return content;
    }

    public static UIElement AddToolTip(this UIElement content, string tip)
    {
        ToolTip toolTip = new();
        toolTip.Content = tip;

        return AddToolTip(content, toolTip);
    }

    public static Grid AddToolTip(this Grid content, string tip)
    {
        ToolTip toolTip = new();
        toolTip.Content = tip;

        return AddToolTip(content, toolTip);
    }

    public static StackPanel AddStackTransition(this StackPanel stack)
    {
        var transition = new TransitionCollection { new EntranceThemeTransition() { IsStaggeringEnabled = true } };
        stack.ChildrenTransitions = transition;

        return stack;
    }

    public static Grid WrapInGrid(this UIElement content, ToolTip toolTip = null)
    {
        Grid grid = new();

        grid.Children.Add(content);

        if (toolTip is not null)
            ToolTipService.SetToolTip(grid, toolTip);

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

    public static Grid WrapInExpanderWithToggleButton(this Grid content, ref Grid reference, string source)
    {
        Grid grid = new() { Margin = new(0, 0, 0, 2) };
        Expander expander = new()
        {
            Padding = new(15),
            ExpandDirection = ExpandDirection.Down,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        ToggleButton toggleButton = new() { Content = source.ToString().FormatString(), IsChecked = true };

        expander.Header = toggleButton;
        expander.Content = content;

        grid.Children.Add(expander);

        reference = grid;

        Binding.Get("IsEnabled" + source)?.Set(toggleButton, "IsChecked", "Click");

        return grid;
    }

    public static Grid WrapInExpanderWithToggleButton(this Grid content, SceneEntry sceneEntry, string name = null)
    {
        Grid grid = new() { Margin = new(0, 0, 0, 2) };
        Expander expander = new()
        {
            Padding = new(15),
            ExpandDirection = ExpandDirection.Down,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };
        ToggleButton toggleButton = new()
        {
            Content = name is not null ? name : sceneEntry.Name,
            IsChecked = true
        };

        expander.Header = toggleButton;
        expander.Content = content;

        grid.Children.Add(expander);

        sceneEntry.Content = grid;

        Binding.Get("IsEnabled" + sceneEntry.ID, Binding.SceneBindings)?.Set(toggleButton, "IsChecked", "Click");
        Binding.Get("Name" + sceneEntry.ID, Binding.SceneBindings)?.Set(toggleButton, "Content");

        return grid;
    }

    public static Grid WrapInExpanderWithEditableHeader(this Grid content, string text)
    {
        Grid grid = new();
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

    public static Grid WrapInExpanderWithEditableHeaderAndCheckBox(this Grid content, string text, bool isChecked = true, TextChangedEventHandler onTextChanged = null, RoutedEventHandler onClick = null)
    {
        Grid grid = new();
        Expander expander = new()
        {
            Padding = new(15),
            ExpandDirection = ExpandDirection.Down,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Stretch
        };

        Grid grid2 = new();
        CheckBox checkBox = new() { Content = "", IsChecked = isChecked };
        TextBox textBox = new() { Text = text, Margin = new(25, 0, 0, 0), Height = 23 };
        checkBox.Click += onClick;
        textBox.TextChanged += onTextChanged;

        grid2.Children.Add(checkBox);
        grid2.Children.Add(textBox);

        expander.Header = grid2;
        expander.Content = content;

        grid.Children.Add(expander);

        return grid;
    }

    public static UIElement AddContentFlyout(this UIElement content, FlyoutBase flyout)
    {
        content.ContextFlyout = flyout;

        return content;
    }

    public static async void CreateDialogAsync(this ContentDialog contentDialog) =>
        await contentDialog.ShowAsync();

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

    public static string SplitLast(this string text, char seperator) =>
        text.Split(seperator).Last();

    public static string FirstCharToUpper(this string input) =>
        string.Concat(input[0].ToString().ToUpper(), input.AsSpan(1));

    public static string FormatString(this string text) =>
        text.SplitLast('_').SplitLast('.').SplitLast('+').FirstCharToUpper().AddSpacesToSentence();

    public static string IncrementNameIfExists(this string name, string[] list)
    {
        var i = 0;
        bool nameWithoutIncrement = list.Contains(name);

        foreach (var s in list)
            if (s == name || s.Contains(name + " ("))
                i++;

        if (i > 0 && nameWithoutIncrement)
            name += " (" + (i + 1).ToString() + ")";

        return name;
    }

    public static string IncrementPathIfExists(this string path, string[] list)
    {
        var name = Path.GetFileNameWithoutExtension(path);

        name = name.IncrementNameIfExists(list);

        return Path.Combine(Path.GetDirectoryName(path), name + Path.GetExtension(path));
    }

    public static bool? IsFileLocked(this string path)
    {
        if (!File.Exists(path))
            return null;

        try
        {
            FileInfo file = new FileInfo(path);
            using (FileStream stream = file.Open(FileMode.Open, FileAccess.Read, FileShare.None))
                stream.Close();
        }
        catch (IOException)
        {
            //the file is unavailable because it is:
            //still being written to
            //or being processed by another thread
            //or does not exist (has already been processed)
            return true;
        }

        //file is not locked
        return false;
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

    public BooleanToRowHeightConverter(GridLength initialValue) =>
        _initialValue = initialValue;

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
