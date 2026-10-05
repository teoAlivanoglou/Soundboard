using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Soundboard.Avalonia.UI.Converters;

public class OuterGlowConverter : IValueConverter, IMultiValueConverter
{
    private static ImmutableDropShadowEffect CreateGlow(Color color)
    {
        return new ImmutableDropShadowEffect(0, 0, 32, color, 0.45);
    }

    /// <summary>
    /// Creates an Outer Glow effect using Drop Shadow from a Color or SolidColorBrush.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value switch
        {
            Color c => c,
            SolidColorBrush b => b.Color,
            _ => Colors.Blue
        };

        return CreateGlow(color);
    }

    /// <summary>
    /// Creates an Outer Glow effect using Drop Shadow.
    /// <param name="values[0]">can be bound to a <c>bool</c> (controls whether it returns drop shadow or null), or directly to a Brush/Color.</param>
    /// <param name="values[1]">when values[0] is bool, must be bound to an <c>Avalonia.Color</c> or <c>Avalonia.Media.SolidColorBrush</c>.</param>
    /// </summary>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count == 0)
            return null;

        if (values[0] is bool isPlaying)
        {
            if (!isPlaying)
                return null;

            var source = values.Count > 1 ? values[1] : null;
            return Convert(source, targetType, parameter, culture);
        }

        return Convert(values[0], targetType, parameter, culture);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}