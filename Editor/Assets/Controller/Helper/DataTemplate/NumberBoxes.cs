using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml;
using Microsoft.UI;
using System.Numerics;

using static Editor.Controller.Helper;

namespace Editor.Controller;

public sealed class NumberBoxVector3
{
    public event RoutedEventHandler ValueChanged;
    public Vector3 Value
    {
        get => new Vector3(
            (float)_numberBoxX.Value,
            (float)_numberBoxY.Value,
            (float)_numberBoxZ.Value);
        set
        {
            _numberBoxX.Value = value.X;
            _numberBoxY.Value = value.Y;
            _numberBoxZ.Value = value.Z;
        }
    }

    private NumberBox _numberBoxX;
    private NumberBox _numberBoxY;
    private NumberBox _numberBoxZ;

    private Rectangle _rectangleR;
    private Rectangle _rectangleG;
    private Rectangle _rectangleB;

    public NumberBoxVector3()
    {
        _numberBoxX = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64 };
        _numberBoxY = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64 };
        _numberBoxZ = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64 };

        _numberBoxX.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);
        _numberBoxY.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);
        _numberBoxZ.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);

        _rectangleR = new() { Fill = new SolidColorBrush(Colors.IndianRed), RadiusX = 2, RadiusY = 2, Width = 4 };
        _rectangleG = new() { Fill = new SolidColorBrush(Colors.SeaGreen), RadiusX = 2, RadiusY = 2, Width = 4 };
        _rectangleB = new() { Fill = new SolidColorBrush(Colors.DodgerBlue), RadiusX = 2, RadiusY = 2, Width = 4 };
    }

    public Grid GetStackInGrid() =>
        StackInGrid(_numberBoxX, _numberBoxY, _numberBoxZ);

    public Grid GetStackInGridWithRGB() =>
        StackInGrid(_rectangleR, _numberBoxX, _rectangleG, _numberBoxY, _rectangleB, _numberBoxZ);
}

public sealed class NumberBoxVector2
{
    public event RoutedEventHandler ValueChanged;
    public Vector2 Value
    {
        get => new Vector2(
            (float)_numberBoxX.Value,
            (float)_numberBoxY.Value);
        set
        {
            _numberBoxX.Value = value.X;
            _numberBoxY.Value = value.Y;
        }
    }

    private NumberBox _numberBoxX;
    private NumberBox _numberBoxY;

    private Rectangle _rectangleR;
    private Rectangle _rectangleG;

    public NumberBoxVector2()
    {
        _numberBoxX = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64 };
        _numberBoxY = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64 };

        _numberBoxX.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);
        _numberBoxY.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);

        _rectangleR = new() { Fill = new SolidColorBrush(Colors.IndianRed), RadiusX = 2, RadiusY = 2, Width = 4 };
        _rectangleG = new() { Fill = new SolidColorBrush(Colors.SeaGreen), RadiusX = 2, RadiusY = 2, Width = 4 };
    }

    public Grid GetStackInGrid() =>
        StackInGrid(_numberBoxX, _numberBoxY);

    public Grid GetStackInGridWithRG() =>
        StackInGrid(_rectangleR, _numberBoxX, _rectangleG, _numberBoxY);
}
