using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml;
using System;

namespace Editor.Controller;

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
