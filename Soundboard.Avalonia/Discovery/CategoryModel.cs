using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Avalonia.Utils;
using System;
using System.Collections.Generic;

namespace Soundboard.Avalonia.Discovery;

public partial class CategoryModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    public string RelativePath { get; set; } = string.Empty;

    [ObservableProperty] public partial int SoundCount { get; set; }
    [ObservableProperty] public partial int DirectSoundCount { get; set; }
    
    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial bool IsExpanded { get; set; } = true;

    [ObservableProperty] public partial ImmutableSolidColorBrush BackgroundBrushDark { get; set; }
    [ObservableProperty] public partial ImmutableSolidColorBrush BackgroundBrushLight { get; set; }

    public CategoryModel? Parent { get; set; }
    public List<CategoryModel> Children { get; set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasPrefix))]
    public partial string Prefix { get; set; } = string.Empty;

    public bool HasPrefix => !string.IsNullOrEmpty(Prefix);
    public bool IsRootLabel => Name == "(root)";
    public bool HasColor => Color.HasValue;

    /// <summary>                                                                                                                                                                 
    /// Returns the theme-appropriate brush for the current active theme variant.                                                                                                 
    /// </summary>                                                                                                                                                                
    public ImmutableSolidColorBrush BackgroundBrush =>
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

    public CategoryModel(string name, int soundCount)
        : this(name, soundCount, new ImmutableSolidColorBrush(Colors.Transparent), new ImmutableSolidColorBrush(Colors.Transparent))
    {
        Color = null;
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

    public CategoryModel(string name, int soundCount, ImmutableSolidColorBrush backgroundBrushDark,
        ImmutableSolidColorBrush backgroundBrushLight, bool isAll = false)
    {
        Name = name;
        SoundCount = soundCount;
        IsEnabled = true;
        BackgroundBrushDark = backgroundBrushDark;
        BackgroundBrushLight = backgroundBrushLight;
        IsAll = isAll;
    }

    public void NotifyThemeChanged()
    {
        OnPropertyChanged(nameof(BackgroundBrush));
        foreach (var child in Children)
        {
            child.NotifyThemeChanged();
        }
    }

    /// <summary>
    /// Design-time constructor with sample category data.
    /// </summary>
    public CategoryModel() : this("Basses", 12, ColorPalette.GetColor(0))
    {
    }
}