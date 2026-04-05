using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace SoftScroll;

public class NullToVisibilityConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value == null || (value is string s && string.IsNullOrEmpty(s)))
            return Visibility.Collapsed;
        return Visibility.Visible;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}

public class VolumeConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is float f)
            return (double)f * 100;
        if (value is double d)
            return d * 100;
        return 50.0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double d)
            return (float)(d / 100.0);
        return 0.5f;
    }
}
