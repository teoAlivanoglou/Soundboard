using System.Globalization;
using System.Windows.Data;

namespace Soundboard.UI.Converters;

public class AdaptiveFontSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string text || string.IsNullOrWhiteSpace(text))
        {
            return 12;
        }

        return text.Length switch
        {
            <= 80 => 12,
            // <= 114 => 10,
            _ => 10
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}