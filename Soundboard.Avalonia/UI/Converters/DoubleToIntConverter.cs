using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Soundboard.Avalonia.UI.Converters;

public class DoubleToIntConverter : IValueConverter
{
    public static readonly DoubleToIntConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            int i => (double)i,
            double d => d,
            _ => 0.0
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            double d => (int)Math.Round(d),
            int i => i,
            _ => 0
        };
    }
}
