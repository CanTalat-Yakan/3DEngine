using CommunityToolkit.WinUI.UI.Controls;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media.Animation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml;
using System.Numerics;
using System;

using ColorPicker = CommunityToolkit.WinUI.UI.Controls.ColorPicker;
using ExpandDirection = Microsoft.UI.Xaml.Controls.ExpandDirection;
using Expander = Microsoft.UI.Xaml.Controls.Expander;
using FontFamily = Microsoft.UI.Xaml.Media.FontFamily;
using Image = Microsoft.UI.Xaml.Controls.Image;
using Orientation = Microsoft.UI.Xaml.Controls.Orientation;
using Windows.Globalization.NumberFormatting;

namespace Editor.Controller;

internal partial class Helper
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
}

internal partial class Helper
{
    // TODO: Add Bindings to every method in this partial class, to be moved into the next partial class.
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

    internal static UIElement CreateTextFull(out TextBlock textBlock) =>
        textBlock = new();

    internal static UIElement CreateText(string placeholder = "Example") =>
        new TextBlock() { Text = placeholder, MaxWidth = 200, TextWrapping = TextWrapping.Wrap };

    internal static UIElement CreateTextEqual(string placeholder = "Example") =>
        new TextBlock() { Text = placeholder, MaxWidth = 200 };

    internal static UIElement CreateTextFull(string s = "String") =>
        new TextBlock() { Text = s, TextWrapping = TextWrapping.Wrap };

    internal static UIElement CreateTextFullWithOpacity(string s = "String") =>
        new TextBlock() { Text = s, Opacity = 0.5f, TextWrapping = TextWrapping.Wrap };

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
        Windows.UI.Color col = new() { R = r, G = g, B = b, A = a };

        ColorPickerButton colbutton = new() { SelectedColor = col };

        Style stylee = new() { TargetType = typeof(ColorPicker) };
        stylee.Setters.Add(new Setter(ColorPicker.ColorSpectrumShapeProperty, ColorSpectrumShape.Ring));
        stylee.Setters.Add(new Setter(ColorPicker.IsAlphaEnabledProperty, true));
        stylee.Setters.Add(new Setter(ColorPicker.IsHexInputVisibleProperty, true));
        colbutton.ColorPickerStyle = stylee;

        return StackInGrid(colbutton);
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
}

internal partial class Helper
{
    internal static Grid CreateTextWithOpacity(object id, object source, string fieldName, string s = "String")
    {
        TextBlock textBlock = new() { Text = s, MaxWidth = 200, Opacity = 0.5f, TextWrapping = TextWrapping.Wrap };

        Binding.GetBinding(fieldName, source, id)?.Set(textBlock, "Text");

        return StackInGrid(textBlock);
    }

    internal static Grid CreateComboBox(Type enumType, object id, object source, string fieldName, string value = null)
    {
        ComboBox comboBox = new() { Height = 33, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };

        foreach (var item in Enum.GetNames(enumType))
            comboBox.Items.Add(item);

        if (!string.IsNullOrEmpty(value))
            if (Enum.TryParse(enumType, value, out var selectedItem))
                comboBox.SelectedIndex = Array.IndexOf(Enum.GetValues(enumType), selectedItem);

        Binding.GetBinding(fieldName, source, id)?.Set(comboBox, "SelectedItem", "SelectionChanged");

        return StackInGrid(comboBox);
    }

    internal static Grid CreateTextInput(object id, object source, string fieldName, string placeholder = "Example")
    {
        TextBox textBox = new() { Text = placeholder, MaxWidth = 200 };

        Binding.GetBinding(fieldName, source, id)?.Set(textBox, "Text", "TextChanging");

        return StackInGrid(textBox);
    }

    internal static Grid CreateNumberInput(object id, object source, string fieldName, float value = 0, float min = float.MinValue, float max = float.MaxValue)
    {
        IncrementNumberRounder rounder = new();
        rounder.Increment = 0.0001;
        rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

        DecimalFormatter formatter = new();
        formatter.IntegerDigits = 1;
        formatter.FractionDigits = 2;
        formatter.NumberRounder = rounder;

        NumberBox numberBox = new() { Value = value, Minimum = min, Maximum = max, MaxWidth = 200, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };
        numberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact;

        Binding.GetBinding(fieldName, source, id)?.Set(numberBox, "Value", "ValueChanged");

        return StackInGrid(numberBox);
    }

    internal static Grid CreateNumberInputInt(object id, object source, string fieldName, float value = 0, float min = int.MinValue, float max = int.MaxValue)
    {
        IncrementNumberRounder rounder = new();
        rounder.Increment = 1;
        rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

        DecimalFormatter formatter = new();
        formatter.IntegerDigits = 1;
        formatter.FractionDigits = 0;
        formatter.NumberRounder = rounder;

        NumberBox numberBox = new() { Value = value, Minimum = min, Maximum = max, MaxWidth = 200, SmallChange = 1, LargeChange = 10, NumberFormatter = formatter };
        numberBox.SpinButtonPlacementMode = NumberBoxSpinButtonPlacementMode.Compact;

        Binding.GetBinding(fieldName, source, id)?.Set(numberBox, "Value", "ValueChanged");

        return StackInGrid(numberBox);
    }

    internal static Grid CreateSliderInt(object id, object source, string fieldName, double value = 0, double min = 0, double max = 100)
    {
        Slider slider = new() { Value = value, Minimum = min, Maximum = max, StepFrequency = 1, Width = 180, Margin = new(0, 0, 0, -5.5) };
        TextBlock numberPreview = new() { Padding = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

        Binding.GetBinding(fieldName, source, id)?.Set(slider, "Value", "ValueChanged");
        Binding.GetBinding(fieldName, source, id)?.SetEvent(() =>
            numberPreview.Text = Binding.GetBinding(fieldName, source, id)?.Value.ToString());

        return StackInGrid(slider, numberPreview);
    }

    internal static Grid CreateSlider(object id, object source, string fieldName, double value = 0, double min = 0, double max = 100)
    {
        Slider slider = new() { Value = value, Minimum = min, Maximum = max, StepFrequency = 0.1f, Width = 180, Margin = new(0, 0, 0, -5.5) };
        TextBlock numberPreview = new() { Padding = new(4, 0, 0, 0), VerticalAlignment = VerticalAlignment.Center };

        Binding.GetBinding(fieldName, source, id)?.Set(slider, "Value", "ValueChanged");
        Binding.GetBinding(fieldName, source, id)?.SetEvent(() =>
            numberPreview.Text = Binding.GetBinding(fieldName, source, id)?.Value.ToString());

        return StackInGrid(slider, numberPreview);
    }

    internal static Grid CreateVec2Input(object id, object source, string fieldName, Vector2 value = new())
    {
        NumberBoxVector2 numberBoxVector2 = new() { Value = value };

        Binding.GetBinding(fieldName, source, id)?.Set(numberBoxVector2, "Value", "ValueChanged");

        return numberBoxVector2.GetStackInGrid();
    }

    internal static Grid CreateVec2InputWithRG(object id, object source, string fieldName, Vector2 value = new())
    {
        NumberBoxVector2 numberBoxVector2 = new() { Value = value };

        Binding.GetBinding(fieldName, source, id)?.Set(numberBoxVector2, "Value", "ValueChanged");

        return numberBoxVector2.GetStackInGridWithRG();
    }

    internal static Grid CreateVec3Input(object id, object source, string fieldName, Vector3 value = new())
    {
        NumberBoxVector3 numberBoxVector3 = new() { Value = value };

        Binding.GetBinding(fieldName, source, id)?.Set(numberBoxVector3, "Value", "ValueChanged");

        return numberBoxVector3.GetStackInGrid();
    }

    internal static Grid CreateVec3InputWithRGB(object id, object source, string fieldName, Vector3 value = new())
    {
        NumberBoxVector3 numberBoxVector3 = new() { Value = value };

        Binding.GetBinding(fieldName, source, id)?.Set(numberBoxVector3, "Value", "ValueChanged");

        return numberBoxVector3.GetStackInGridWithRGB();
    }

    internal static Grid CreateVec4Input(object id, object source, string fieldName, Vector4 value = new())
    {
        NumberBoxVector4 numberBoxVector4 = new() { Value = value };

        Binding.GetBinding(fieldName, source, id)?.Set(numberBoxVector4, "Value", "ValueChanged");

        return numberBoxVector4.GetStackInGrid();
    }

    internal static Grid CreateVec4InputWithRGB(object id, object source, string fieldName, Vector4 value = new())
    {
        NumberBoxVector4 numberBoxVector4 = new() { Value = value };

        Binding.GetBinding(fieldName, source, id)?.Set(numberBoxVector4, "Value", "ValueChanged");

        return numberBoxVector4.GetStackInGridWithRGB();
    }

    internal static Grid CreateQuaternionInputWithRGBFromEuler(object id, object source, string fieldName, Quaternion value = new())
    {
        NumberBoxQuaternionToEuler numberBoxVector3 = new() { Value = value };

        Binding.GetBinding(fieldName, source, id)?.Set(numberBoxVector3, "Value", "ValueChanged");

        return numberBoxVector3.GetStackInGridWithRGB();
    }

    internal static Grid CreateBool(object id, object source, string fieldName, bool value = false)
    {
        CheckBox checkBox = new() { IsChecked = value, Margin = new(0, 0, 0, -5.5) };

        Binding.GetBinding(fieldName, source, id)?.Set(checkBox, "IsChecked", "Click");

        return StackInGrid(checkBox);
    }
}

internal partial class Helper
{
    internal static Grid CreateHeader(string s = "Header")
    {
        Grid grid = new();
        TextBlock header = new() { Text = s, FontSize = 18, FontWeight = FontWeights.Bold, Margin = new(0, 5, 0, 5) };

        grid.Children.Add(header);

        return grid;
    }

    internal static Grid CreateSpacer() =>
        new Grid() { Height = 20 };

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

    internal static string GetAppVersion()
    {
        var package = Windows.ApplicationModel.Package.Current;
        var packageId = package.Id;
        var version = packageId.Version;

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

internal static partial class ExtensionMethods
{
    public static UIElement AddContentFlyout(this UIElement content, FlyoutBase flyout)
    {
        content.ContextFlyout = flyout;

        return content;
    }

    public static async void CreateDialogAsync(this ContentDialog contentDialog) =>
        await contentDialog.ShowAsync();

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

    public static Grid WrapInExpander(this Grid content, string text, bool isExpanded = true)
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
        expander.IsExpanded = isExpanded;
        expander.Content = content;

        grid.Children.Add(expander);

        return grid;
    }
}

internal static partial class ExtensionMethods
{
    public static Grid WrapInExpanderWithToggleButton(this Grid content, ref Grid reference,
        object id, object source, bool bindScene = false)
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

        if (bindScene)
        {
            Binding.GetSceneBinding("IsEnabled", id)?.Set(toggleButton, "IsChecked", "Click");
            Binding.GetSceneBinding("Name", id)?.Set(toggleButton, "Content");
        }
        else
            Binding.GetBinding("IsEnabled", source, id)?.Set(toggleButton, "IsChecked", "Click");

        return grid;
    }

    public static Grid WrapInExpanderWithEditableHeader(this Grid content,
        object id, string text)
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

        Binding.GetEntityBinding("Name", id)?.Set(expander, "Header");

        return grid;
    }

    public static Grid WrapInExpanderWithEditableHeaderAndCheckBox(this Grid content,
        object id, string text, bool isChecked = true)
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

        grid2.Children.Add(checkBox);
        grid2.Children.Add(textBox);

        expander.Header = grid2;
        expander.Content = content;

        grid.Children.Add(expander);

        Binding.GetEntityBinding("IsEnabled", id)?.Set(checkBox, "IsChecked", "Click");
        Binding.GetEntityBinding("Name", id)?.Set(textBox, "Text", "TextChanging");

        return grid;
    }
}
