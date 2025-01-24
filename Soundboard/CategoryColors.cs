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
        public static List<Color> Palette { get; }

        static CategoryColors()
        {
            Palette =
            [
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

            CategoriesColors = new Dictionary<(string, int), int>();
        }

        public static Color GetColorForCategory(string category, int group = 0)
        {
            try
            {

                if (string.IsNullOrWhiteSpace(category) || category == ".")
                    category = " ";

                if (CategoriesColors.TryGetValue((category, group), out var categoriesColor))
                {
                    return Palette[categoriesColor % (Palette.Count)];
                }

                var color = Palette[CategoriesColors.Count % (Palette.Count)];
                CategoriesColors.Add((category, group), CategoriesColors.Count);
                return color;

            }
            catch
            {
                throw new Exception("WTF");
            }
        }

        public static Color ColorFromHex(string hexValue)
        {
            // uint number2;
            // uint.TryParse(hexValue.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out number2);

            return uint.TryParse(hexValue.TrimStart('#'), System.Globalization.NumberStyles.HexNumber, null, out var num) 
                ? Color.FromRgb((byte)((num >> 16) & 0xFF), (byte)((num >> 8) & 0xFF), (byte)(num & 0xFF)) 
                : Colors.Magenta;
        }
    }
}