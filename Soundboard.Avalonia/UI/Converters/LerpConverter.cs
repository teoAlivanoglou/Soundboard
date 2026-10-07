using System;
using System.Collections.Generic;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Soundboard.Avalonia.UI.Converters;

public class LerpConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count != 2) return 0.0;

        if (values[0] is double d1 && values[1] is double d2)
        {
            return double.IsFinite(d1) && double.IsFinite(d2) ? Math.Max(0.0, d1 * d2) : 0.0;
        }
        
        var a = values[0] switch
        {
            double d when !double.IsNaN(d) && !double.IsInfinity(d) => d,
            int i => (double)i,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0.0
        };

        var b = values[1] switch
        {
            double d when !double.IsNaN(d) && !double.IsInfinity(d) => d,
            int i => (double)i,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0.0
        };

        return Math.Max(0.0, a * b);
    }
}