using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Avalonia.Utils;

namespace Soundboard.Avalonia.Discovery;

public partial class CategoryModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial int SoundCount { get; set; }
    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial ISolidColorBrush BackgroundBrushDark { get; set; }
    [ObservableProperty] public partial ISolidColorBrush BackgroundBrushLight { get; set; }

                                                                                                                                                                                   
    /// <summary>                                                                                                                                                                 
    /// Returns the theme-appropriate brush for the current active theme variant.                                                                                                 
    /// </summary>                                                                                                                                                                
    public ISolidColorBrush BackgroundBrush =>                                                                                                                                    
        Application.Current?.ActualThemeVariant == ThemeVariant.Light                                                                                                             
            ? BackgroundBrushLight                                                                                                                                                
            : BackgroundBrushDark; 

    public Color? Color { get; init; }
    public bool IsAll { get; init; }

    public static CategoryModel All { get; } = new("All", 0, ColorPalette.GetColor(^1), isAll: true);

    public static CategoryModel GetAll(int soundCount = 0)
    {
        All.SoundCount = soundCount;
        All.IsEnabled = true;
        return All;
    }

    public CategoryModel(string name, int soundCount, Color color, bool isAll = false)
        : this(name, soundCount, HelmlabColorConversion.GetDualModeBrushes(color), isAll)
    {
        Color = color;
    }

    public CategoryModel(string name, int soundCount, ImmutableSolidColorBrushPair brushes, bool isAll = false)
        : this(name, soundCount, brushes.Dark, brushes.Light, isAll)
    {
    }

    public CategoryModel(string name, int soundCount, ISolidColorBrush backgroundBrushDark, ISolidColorBrush backgroundBrushLight, bool isAll = false)
    {
        Name = name;
        SoundCount = soundCount;
        IsEnabled = true;
        BackgroundBrushDark = backgroundBrushDark;
        BackgroundBrushLight = backgroundBrushLight;
        IsAll = isAll;

        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged += OnThemeVariantChanged;
        }
    }
    private void OnThemeVariantChanged(object? sender, EventArgs e)
    {
        OnPropertyChanged(nameof(BackgroundBrush));
    }

    /// <summary>
    /// Design-time constructor with sample category data.
    /// </summary>
    public CategoryModel() : this("Basses", 12, ColorPalette.GetColor(0))
    {
    }
}