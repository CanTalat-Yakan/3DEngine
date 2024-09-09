using System.Numerics;

using CommunityToolkit.WinUI.Controls;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml;

using ColorPicker = CommunityToolkit.WinUI.Controls.ColorPicker;

namespace Editor.Controller;

public sealed class ColorButton
{
    public event RoutedEventHandler ValueChanged;

    public Vector4 SelectedColor
    {
        get => ColorPickerButton.SelectedColor.ToVector4();
        set => ColorPickerButton.SelectedColor = value.ToColor();
    }

    public ColorPickerButton ColorPickerButton;

    public ColorButton()
    {
        ColorPickerButton = new();

        Style style = new() { TargetType = typeof(ColorPicker) };
        style.Setters.Add(new Setter(ColorPicker.ColorSpectrumShapeProperty, ColorSpectrumShape.Ring));
        style.Setters.Add(new Setter(ColorPicker.IsAlphaEnabledProperty, true));
        style.Setters.Add(new Setter(ColorPicker.IsHexInputVisibleProperty, true));
        ColorPickerButton.ColorPickerStyle = style;

        ColorPickerButton.Click += (s, e) => ColorPickerButton.ColorPicker.ColorChanged += (s, e) => ValueChanged?.Invoke(this, null);
    }

    public Grid GetStackInGrid() =>
       Helper.StackInGrid(ColorPickerButton);
}
