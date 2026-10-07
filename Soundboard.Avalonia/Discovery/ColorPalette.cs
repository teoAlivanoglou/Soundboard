using System;
using System.Globalization;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Soundboard.Avalonia.Utils;

namespace Soundboard.Avalonia.Discovery;

public static class ColorPalette
{
    private static readonly Color[] PrimaryColors =
    [
        Color.FromHex("#FFFBBF24"), // Warm Gold               
        Color.FromHex("#FF34D399"), // Cool Mint Green         
        Color.FromHex("#FF38BDF8"), // Cool Sky Blue           
        Color.FromHex("#FFF472B6"), // Warm Pink               
        Color.FromHex("#FFFB923C"), // Warm Orange             
        Color.FromHex("#FF94A3B8"), // Cool Slate Gray         
        Color.FromHex("#FFA78BFA"), // Cool Violet             
        Color.FromHex("#FFF87171"), // Warm Coral Red          
        Color.FromHex("#FF2DD4BF"), // Cool Teal               
        Color.FromHex("#FFE879F9"), // Electric Fuchsia        
        Color.FromHex("#FF60A5FA"), // Vivid Blue              
        Color.FromHex("#FFFB7185"), // Warm Rose               
        Color.FromHex("#FF4ADE80"), // Fresh Spring Green      
        Color.FromHex("#FFC084FC"), // Orchid Purple           
        Color.FromHex("#FF22D3EE"), // Electric Cyan           
        Color.FromHex("#FFA3E635"), // Bright Chartreuse / Lime
        Color.FromHex("#FF818CF8")  // Indigo (All button)
    ];

    public static Color GetColor(Index index) => PrimaryColors[index.IsFromEnd ? index : index.Value  % PrimaryColors.Length];

    extension(Color)
    {
        public static Color FromHex(string hexValue) =>
            uint.TryParse(hexValue.TrimStart('#'), NumberStyles.HexNumber, null, out var num)
                ? Color.FromRgb(
                    (byte)((num >> 16) & 0xFF),
                    (byte)((num >> 8) & 0xFF),
                    (byte)(num & 0xFF))
                : Colors.Magenta;
    }
}