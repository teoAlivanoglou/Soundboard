using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using Avalonia.Data.Converters;

namespace Soundboard.Avalonia.UI.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public static readonly EnumDescriptionConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Enum enumValue)
        {
            var field = enumValue.GetType().GetField(enumValue.ToString());
            if (field?.GetCustomAttribute<DescriptionAttribute>() is { } desc)
            {
                return desc.Description;
            }

            return enumValue.ToString();
        }

        return value?.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
