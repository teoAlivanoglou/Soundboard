using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Soundboard.Avalonia.UI.Converters;

public class OuterGlowConverter : IMultiValueConverter
{
    /// <summary>
    /// Creates an Outer Glow effect using Drop Shadow.
    /// <param name="values[0]">must be bound to a <c>bool</c> and controls whether it returns a drop shadow or null.</param>
    /// <param name="values[1]">must be bound to an <c>Avalonia.Color</c> or <c>Avalonia.Media.SolidColorBrush</c>.</param>
    /// </summary>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values is not [true, var source])
            return null;

        var color = source switch
        {
            Color c => c,
            SolidColorBrush b => b.Color,
            _ => Colors.Blue
        };

        return new ImmutableDropShadowEffect(0, 0, 32, color, 0.45);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}