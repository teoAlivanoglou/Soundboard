using System.Collections.Generic;
using Avalonia.Media;
using Avalonia.Media.Immutable;

namespace Soundboard.Avalonia.Discovery;

public static class ColorPalette
{
    public static readonly ImmutableSolidColorBrush AllButtonBrush = new(Color.FromHex("#FF818CF8"));

    private static readonly List<Color> PrimaryColors =
    [
        Color.FromHex("#FFFBBF24"),
        Color.FromHex("#FF34D399"),
        Color.FromHex("#FF38BDF8"),
        Color.FromHex("#FFF472B6"),
        Color.FromHex("#FFFB923C"),
        Color.FromHex("#FF94A3B8"),
        Color.FromHex("#FFD1DC"), // Rose
        Color.FromHex("#D1C4E9"), // Deep Lavender
        Color.FromHex("#BBDEFB"), // Sky Blue
        Color.FromHex("#B2DFDB"), // Mint
        Color.FromHex("#DCEDC8"), // Soft Lime
        Color.FromHex("#FFF9C4"), // Light Yellow
        Color.FromHex("#FFE0B2"), // Apricot
        Color.FromHex("#D7CCC8"), // Warm Grey
    ];

    public static ImmutableSolidColorBrush GetBrush(int index)
    {
        var color = PrimaryColors[index % PrimaryColors.Count];
        return new ImmutableSolidColorBrush(color);
    }

    private static int _nextIndex = 0;
    public static ImmutableSolidColorBrush GetBrush() => GetBrush(_nextIndex++);


    extension(Color)
    {
        public static Color FromHex(string hexValue) =>
            uint.TryParse(hexValue.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out var num)
                ? Color.FromRgb(
                    (byte)((num >> 16) & 0xFF),
                    (byte)((num >> 8) & 0xFF),
                    (byte)(num & 0xFF))
                : Colors.Magenta;
    }
}