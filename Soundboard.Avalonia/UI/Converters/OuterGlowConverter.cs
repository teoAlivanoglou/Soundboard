using Avalonia.Data.Converters;
using Avalonia.Media;
using Avalonia;
using Avalonia.Styling;
using Avalonia.Threading;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace Soundboard.Avalonia.UI.Converters;

public class OuterGlowConverter : IValueConverter, IMultiValueConverter
{
    private static ImmutableDropShadowEffect CreateGlow(Color color, string parameter)
    {
        var isLight = false;
        if (Dispatcher.UIThread.CheckAccess() && Application.Current is not null)
        {
            isLight = Application.Current.ActualThemeVariant == ThemeVariant.Light;
        }
        
        double blur;
        double opacity;

        switch (parameter)
        {
            case "PlayheadOuter":
                blur = 8;
                opacity = isLight ? 0.9 : 0.85;
                break;
            case "PlayheadInner":
                blur = 5;
                opacity = isLight ? 0.9 : 0.8;
                break;
            case "Pad":
            default:
                blur = isLight ? 20 : 24;
                opacity = isLight ? 0.7 : 0.55;
                break;
        }

        return new ImmutableDropShadowEffect(0, 0, blur, color, opacity);
    }

    private static Color GetColorSafe(ISolidColorBrush brush)
    {
        try
        {
            return brush.Color;
        }
        catch (InvalidOperationException)
        {
            return Colors.Blue;
        }
    }

    /// <summary>
    /// Creates an Outer Glow effect using Drop Shadow from a Color or SolidColorBrush.
    /// </summary>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var color = value switch
        {
            Color c => c,
            ISolidColorBrush b => GetColorSafe(b),
            _ => Colors.Blue
        };

        var paramStr = parameter as string ?? "Pad";
        return CreateGlow(color, paramStr);
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