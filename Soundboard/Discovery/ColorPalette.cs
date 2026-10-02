using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace Soundboard.Discovery;

public static class ColorPalette
{
    public static readonly Brush AllButtonBrush = new SolidColorBrush(ColorFromHex("#CAC8D6"));
    public static readonly List<Color> PrimaryColors =
    [
        ColorFromHex("#FAEDCB"), // Cream
        ColorFromHex("#C9E4DE"), // Teal
        ColorFromHex("#C6DEF1"), // Soft Blue
        ColorFromHex("#DBCDF0"), // Lavender
        ColorFromHex("#F2C6DE"), // Pink
        ColorFromHex("#F7D9C4"), // Peach
        ColorFromHex("#FFD1DC"), // Rose
        ColorFromHex("#D1C4E9"), // Deep Lavender
        ColorFromHex("#BBDEFB"), // Sky Blue
        ColorFromHex("#B2DFDB"), // Mint
        ColorFromHex("#DCEDC8"), // Soft Lime
        ColorFromHex("#FFF9C4"), // Light Yellow
        ColorFromHex("#FFE0B2"), // Apricot
        ColorFromHex("#D7CCC8"), // Warm Grey
    ];
    public static SolidColorBrush GetBrush(int index)
    {
        var color = PrimaryColors[index % PrimaryColors.Count];
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static int nextIndex = 0;
    public static SolidColorBrush GetBrush() => GetBrush(nextIndex++);

    private static Color ColorFromHex(string hexValue)
    {
        return uint.TryParse(hexValue.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out uint num)
            ? Color.FromRgb(
                (byte)((num >> 16) & 0xFF),
                (byte)((num >> 8) & 0xFF),
                (byte)(num & 0xFF))
            : Colors.Magenta;
    }
}
