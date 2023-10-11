using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Xaml;
using System.Numerics;
using Windows.Globalization.NumberFormatting;

using Engine.Helper;

using Vortice.Mathematics;

using Colors = Microsoft.UI.Colors;

using static Editor.Controller.Helper;

namespace Editor.Controller;

public sealed class NumberBoxQuaternionToEuler
{
    public event RoutedEventHandler ValueChanged;

    public Quaternion Value
    {
        get => new Vector3(
            (float)_numberBoxX.Value,
            (float)_numberBoxY.Value,
            (float)_numberBoxZ.Value)
            .ToRadians().FromEuler();
        set
        {
            var euler = value.ToEuler().ToDegrees();
            _numberBoxX.Value = euler.X;
            _numberBoxY.Value = euler.Y;
            _numberBoxZ.Value = euler.Z;
        }
    }

    private NumberBox _numberBoxX;
    private NumberBox _numberBoxY;
    private NumberBox _numberBoxZ;

    private Rectangle _rectangleR;
    private Rectangle _rectangleG;
    private Rectangle _rectangleB;

    public NumberBoxQuaternionToEuler()
    {
        IncrementNumberRounder rounder = new();
        rounder.Increment = 0.0001;
        rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

        DecimalFormatter formatter = new();
        formatter.IntegerDigits = 1;
        formatter.FractionDigits = 0;
        formatter.NumberRounder = rounder;

        _numberBoxX = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, NumberFormatter = formatter };
        _numberBoxY = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, NumberFormatter = formatter };
        _numberBoxZ = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, NumberFormatter = formatter };

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

public sealed class NumberBoxVector4
{
    public event RoutedEventHandler ValueChanged;

    public Vector4 Value
    {
        get => new Vector4(
            (float)_numberBoxX.Value,
            (float)_numberBoxY.Value,
            (float)_numberBoxZ.Value,
            (float)_numberBoxW.Value);
        set
        {
            _numberBoxX.Value = value.X;
            _numberBoxY.Value = value.Y;
            _numberBoxZ.Value = value.Z;
            _numberBoxW.Value = value.W;
        }
    }

    private NumberBox _numberBoxX;
    private NumberBox _numberBoxY;
    private NumberBox _numberBoxZ;
    private NumberBox _numberBoxW;

    private Rectangle _rectangleR;
    private Rectangle _rectangleG;
    private Rectangle _rectangleB;
    private Rectangle _rectangleA;

    public NumberBoxVector4()
    {
        IncrementNumberRounder rounder = new();
        rounder.Increment = 0.0001;
        rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

        DecimalFormatter formatter = new();
        formatter.IntegerDigits = 1;
        formatter.FractionDigits = 0;
        formatter.NumberRounder = rounder;

        _numberBoxX = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };
        _numberBoxY = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };
        _numberBoxZ = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };
        _numberBoxW = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };

        _numberBoxX.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);
        _numberBoxY.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);
        _numberBoxZ.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);
        _numberBoxW.ValueChanged += (s, e) => ValueChanged?.Invoke(this, null);

        _rectangleR = new() { Fill = new SolidColorBrush(Colors.IndianRed), RadiusX = 2, RadiusY = 2, Width = 4 };
        _rectangleG = new() { Fill = new SolidColorBrush(Colors.SeaGreen), RadiusX = 2, RadiusY = 2, Width = 4 };
        _rectangleB = new() { Fill = new SolidColorBrush(Colors.DodgerBlue), RadiusX = 2, RadiusY = 2, Width = 4 };
        _rectangleA = new() { Fill = new SolidColorBrush(Colors.White), RadiusX = 2, RadiusY = 2, Width = 4 };
    }

    public Grid GetStackInGrid() =>
        StackInGridVertical(
            StackInGrid(_numberBoxX, _numberBoxY), 
            StackInGrid(_numberBoxZ, _numberBoxW));

    public Grid GetStackInGridWithRGB() =>
        StackInGridVertical(
            StackInGrid(_rectangleR, _numberBoxX, _rectangleG, _numberBoxY),
            StackInGrid(_rectangleB, _numberBoxZ, _rectangleA, _numberBoxW));
}

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
        IncrementNumberRounder rounder = new();
        rounder.Increment = 0.0001;
        rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

        DecimalFormatter formatter = new();
        formatter.IntegerDigits = 1;
        formatter.FractionDigits = 0;
        formatter.NumberRounder = rounder;

        _numberBoxX = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };
        _numberBoxY = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };
        _numberBoxZ = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };

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
        IncrementNumberRounder rounder = new();
        rounder.Increment = 0.0001;
        rounder.RoundingAlgorithm = RoundingAlgorithm.RoundHalfUp;

        DecimalFormatter formatter = new();
        formatter.IntegerDigits = 1;
        formatter.FractionDigits = 0;
        formatter.NumberRounder = rounder;

        _numberBoxX = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };
        _numberBoxY = new NumberBox() { Margin = new(0, 0, 4, 0), MaxWidth = 64, Minimum = float.MinValue, Maximum = float.MaxValue, SmallChange = 0.1f, LargeChange = 1f, NumberFormatter = formatter };

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
