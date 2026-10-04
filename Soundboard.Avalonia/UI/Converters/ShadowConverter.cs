using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;
using System;
using System.Globalization;

namespace Soundboard.Avalonia.UI.Converters;

public class ShadowConverter : IValueConverter
{

    /// <summary>
    /// Creates a BoxShadow object.
    /// <param name="value">must be bound to an <c>Avalonia.Media.Color</c> or <c>Avalonia.Media.SolidColorBrush</c>.</param>
    /// <param name="parameter">must be a <c>Avalonia.Thickness</c>, <c>double</c>, <c>int</c> or <c>string</c>.</param>
    /// </summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value switch
        {
            SolidColorBrush brush => brush.Color,
            Color c => c,
            _ => Colors.Transparent
        };

        var width = parameter switch
        {
            Thickness thickness => thickness.Left,
            double d => d,
            int i => i,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 28
        };

        var shadow = new BoxShadow
        {
            IsInset = true,
            OffsetX = width,
            OffsetY = 0,
            Blur = 0,
            Spread = 0,
            Color = color
        };

        return new BoxShadows(shadow);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}