using System;

namespace Soundboard.Avalonia.UI.Controls;

public static class TimeSpanExtensions
{
    public static TimeSpan Ms(this int milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
    public static TimeSpan Ms(this double milliseconds) => TimeSpan.FromMilliseconds(milliseconds);
    public static TimeSpan Seconds(this int seconds) => TimeSpan.FromSeconds(seconds);
    public static TimeSpan Seconds(this double seconds) => TimeSpan.FromSeconds(seconds);
}
