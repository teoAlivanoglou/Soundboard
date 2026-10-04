using System.Windows.Media;

namespace Soundboard.Discovery;

public static class ColorPalette
{
    public static readonly SolidColorBrush AllButtonBrush = new SolidColorBrush(Color.FromHex("#CAC8D6"));

    public static readonly List<Color> PrimaryColors =
    [
        Color.FromHex("#FAEDCB"), // Cream
        Color.FromHex("#C9E4DE"), // Teal
        Color.FromHex("#C6DEF1"), // Soft Blue
        Color.FromHex("#DBCDF0"), // Lavender
        Color.FromHex("#F2C6DE"), // Pink
        Color.FromHex("#F7D9C4"), // Peach
        Color.FromHex("#FFD1DC"), // Rose
        Color.FromHex("#D1C4E9"), // Deep Lavender
        Color.FromHex("#BBDEFB"), // Sky Blue
        Color.FromHex("#B2DFDB"), // Mint
        Color.FromHex("#DCEDC8"), // Soft Lime
        Color.FromHex("#FFF9C4"), // Light Yellow
        Color.FromHex("#FFE0B2"), // Apricot
        Color.FromHex("#D7CCC8"), // Warm Grey
    ];

    public static SolidColorBrush GetBrush(int index)
    {
        var color = PrimaryColors[index % PrimaryColors.Count];
        var brush = new SolidColorBrush(color);
        brush.Freeze();
        return brush;
    }

    private static int _nextIndex = 0;
    public static SolidColorBrush GetBrush() => GetBrush(_nextIndex++);


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