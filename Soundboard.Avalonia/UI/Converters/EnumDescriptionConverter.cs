using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Soundboard.Avalonia.AudioEngine;

namespace Soundboard.Avalonia.UI.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public static readonly EnumDescriptionConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DriverType dt)
        {
            return dt switch
            {
#if WINDOWS
                DriverType.WaveOutEvent => "WaveOut",
                DriverType.Wasapi => "WASAPI",
                DriverType.DirectSound => "DirectSound",
#else
                DriverType.CoreAudio => "CoreAudio",
#endif
                _ => dt.ToString()
            };
        }

        if (value is SampleRate sr)
        {
            return sr switch
            {
                SampleRate.R44100 => "44100",
                SampleRate.R48000 => "48000",
                SampleRate.R88200 => "88200",
                SampleRate.R96000 => "96000",
                SampleRate.R192000 => "192000",
                _ => sr.ToString()
            };
        }

        return value?.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}
