using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace Soundboard
{
    public static class CategoryColors
    {
        public static Dictionary<(string, int), int> CategoriesColors { get; }

        // public static Dictionary<(string, int), int> CategoriesColorsSecondary { get; }
        public static List<Color> PalettePrimary { get; }
        public static List<Color> PaletteSecondary { get; }

        static CategoryColors()
        {
            PalettePrimary =
            [
                // ColorFromHex("#FFFFFF"),
                // ColorFromHex("#fffe51"),
                // ColorFromHex("#77f9fd"),
                // ColorFromHex("#78f94a"),
                // ColorFromHex("#e943f8"),
                // ColorFromHex("#e93f1f"),
                // ColorFromHex("#0014f6"),
                // ColorFromHex("#000000"),
                ColorFromHex("#CAC8D6"),
                ColorFromHex("#FAEDCB"),
                ColorFromHex("#C9E4DE"),
                ColorFromHex("#C6DEF1"),
                ColorFromHex("#DBCDF0"),
                ColorFromHex("#F2C6DE"),
                ColorFromHex("#F7D9C4"),
                ColorFromHex("#FFD1DC"),
                ColorFromHex("#D1C4E9"),
                ColorFromHex("#BBDEFB"),
                ColorFromHex("#B3E5FC"),
                ColorFromHex("#B2EBF2"),
                ColorFromHex("#B2DFDB"),
                ColorFromHex("#DCEDC8"),
                ColorFromHex("#F0F4C3"),
                ColorFromHex("#FFF9C4"),
                ColorFromHex("#FFECB3"),
                ColorFromHex("#FFE0B2"),
                ColorFromHex("#FFCCBC"),
                ColorFromHex("#D7CCC8"),
                ColorFromHex("#F8BBD0"),
                ColorFromHex("#E1BEE7"),
                ColorFromHex("#D1C4E9"),
                ColorFromHex("#C5CAE9"),
                ColorFromHex("#B3E5FC"),
                ColorFromHex("#B2EBF2"),
                ColorFromHex("#C8E6C9"),
                ColorFromHex("#DCEDC8"),
                ColorFromHex("#FFF9C4"),
                ColorFromHex("#FFE0B2"),
                ColorFromHex("#FFCCBC"),
                ColorFromHex("#CFD8DC")
            ];

            PaletteSecondary =
            [
                ColorFromHex("#e2e1e9"),
                ColorFromHex("#fcf5e3"),
                ColorFromHex("#e2f1ed"),
                // ColorFromHex("#ff0000"),
                // ColorFromHex("#00ff00"),
                // ColorFromHex("#0000ff"),

                ColorFromHex("#e0edf7"),
                ColorFromHex("#ece4f7"),
                ColorFromHex("#f8e0ed"),
                ColorFromHex("#faebdf"),
                ColorFromHex("#ffe6ec"),
                ColorFromHex("#e6dff3"),
                ColorFromHex("#daedfc"),
                ColorFromHex("#d5f1fd"),
                ColorFromHex("#d5f4f8"),
                ColorFromHex("#d5eeec"),
                ColorFromHex("#ecf5e1"),
                ColorFromHex("#f7f9de"),
                ColorFromHex("#fffbdf"),
                ColorFromHex("#fff5d5"),
                ColorFromHex("#ffeed5"),
                ColorFromHex("#ffe4da"),
                ColorFromHex("#eae4e1"),
                ColorFromHex("#fbdae6"),
                ColorFromHex("#efdcf2"),
                ColorFromHex("#e6dff3"),
                ColorFromHex("#e0e2f3"),
                ColorFromHex("#d5f1fd"),
                ColorFromHex("#d5f4f8"),
                ColorFromHex("#e1f2e2"),
                ColorFromHex("#ecf5e1"),
                ColorFromHex("#fffbdf"),
                ColorFromHex("#ffeed5"),
                ColorFromHex("#ffe4da"),
                ColorFromHex("#e5eaec"),
            ];


            CategoriesColors = new Dictionary<(string, int), int>();
            // CategoriesColorsSecondary = new Dictionary<(string, int), int>();
        }

        public static Color GetColorForCategory(string category, int group = 0, bool overlay = false)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(category) || category == ".")
                    category = " ";

                if (CategoriesColors.TryGetValue((category, group), out var categoriesColorIndex))
                {
                    var idx = categoriesColorIndex % (PalettePrimary.Count);
                    return overlay
                        ? PaletteSecondary[idx]
                        : PalettePrimary[idx];
                }

                CategoriesColors.Add((category, group), CategoriesColors.Count);

                return overlay
                    ? PaletteSecondary[CategoriesColors.Count % (PalettePrimary.Count)]
                    : PalettePrimary[CategoriesColors.Count % (PalettePrimary.Count)];
            }
            catch
            {
                throw new Exception("WTF");
            }
        }

        public static Color ColorFromHex(string hexValue)
        {
            return uint.TryParse(hexValue.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null,
                out var num)
                ? Color.FromRgb((byte)((num >> 16) & 0xFF), (byte)((num >> 8) & 0xFF), (byte)(num & 0xFF))
                : Colors.Magenta;
        }
    }
}