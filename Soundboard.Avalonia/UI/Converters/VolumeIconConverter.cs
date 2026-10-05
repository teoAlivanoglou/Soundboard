using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace Soundboard.Avalonia.UI.Converters;

public class VolumeIconConverter : IValueConverter
{
    /// <summary>
    /// Provides 1 of 4 volume icons, specifically for lucide. Replace Glyphs and breakpoints for different control
    /// </summary>
    /// <param name="value">We must bind to a volume number. I do 0-100</param>
    /// <returns></returns>
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var volume = value switch
        {
            double d => d,
            int i => i,
            float f => f,
            decimal m => (double)m,
            string s when double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var parsed) => parsed,
            _ => 0.0,
        };


        return volume switch
        {
            // <= 0.8 => "",
            <= 0.8 => "",
            <= 100.0 / 3.0 => "",
            <= 200.0 / 3.0 => "",
            _ => "",
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}