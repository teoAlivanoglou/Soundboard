using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Soundboard.Avalonia.UI.Converters;

public class ShadowConverter : IMultiValueConverter, IValueConverter
{
    private static readonly BoxShadows EmptyPlayingShadow = new(new BoxShadow
    {
        IsInset = true,
        OffsetX = 0,
        OffsetY = 0,
        Blur = 0,
        Spread = 0,
        Color = Colors.Transparent
    });

    private static readonly Dictionary<(Color, double), BoxShadows> ShadowCache = new();

    private static BoxShadows GetOrCreateShadow(Color color, double width, bool isPlaying)
    {
        if (isPlaying)
            return EmptyPlayingShadow;

        var key = (color, width);
        if (ShadowCache.TryGetValue(key, out var cached))
            return cached;

        var shadow = new BoxShadow
        {
            IsInset = true,
            OffsetX = width,
            OffsetY = 0,
            Blur = 0,
            Spread = 0,
            Color = color
        };

        var result = new BoxShadows(shadow);
        ShadowCache[key] = result;
        return result;
    }

    /// <summary>
    /// Creates a BoxShadow object.
    /// <param name="value">must be bound to an <c>Avalonia.Media.Color</c> or <c>Avalonia.Media.SolidColorBrush</c>.</param>
    /// <param name="parameter">must be a <c>Avalonia.Thickness</c>, <c>double</c>, <c>int</c> or <c>string</c>.</param>
    /// </summary>
    private static Color GetColorSafe(ISolidColorBrush brush)
    {
        try
        {
            return brush.Color;
        }
        catch (InvalidOperationException)
        {
            return Colors.Transparent;
        }
    }

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value switch
        {
            ISolidColorBrush brush => GetColorSafe(brush),
            Color c => c,
            _ => Colors.Transparent
        };

        var width = parameter switch
        {
            Thickness thickness => thickness.Left,
            double d => d,
            int i => i,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 3.0
        };

        return GetOrCreateShadow(color, width, isPlaying: false);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Creates a BoxShadow object.
    /// Supports either:
    /// - 2 bindings: [0] = BackgroundBrush, [1] = IsPlaying, with width passed via ConverterParameter
    /// - 4 bindings (legacy): [0] = BackgroundBrush, [1] = PadAccentThickness, [2] = PadAccentCornerRadius, [3] = IsPlaying
    /// </summary>
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        var isPlaying = false;
        object? colorSource = null;
        var width = parameter switch
        {
            Thickness thickness => thickness.Left,
            double d => d,
            int i => i,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 3.0
        };

        if (values.Count >= 2 && values[1] is bool b1)
        {
            // 2-binding format: [0] = Brush, [1] = IsPlaying
            colorSource = values[0];
            isPlaying = b1;
        }
        else if (values.Count > 3 && values[3] is bool b3)
        {
            // Legacy 4-binding format: [0] = Brush, [1] = Thickness, [2] = CornerRadius, [3] = IsPlaying
            colorSource = values[0];
            isPlaying = b3;
            if (values[1] switch
            {
                Thickness t => t.Left,
                double d => d,
                int i => (double)i,
                string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var p) => p,
                _ => (double?)null
            } is double w)
            {
                width = w;
            }
        }
        else
        {
            if (values.Count > 0)
                colorSource = values[0];
        }

        var color = colorSource switch
        {
            ISolidColorBrush brush => GetColorSafe(brush),
            Color c => c,
            _ => Colors.Transparent
        };

        return GetOrCreateShadow(color, width, isPlaying);
    }
}