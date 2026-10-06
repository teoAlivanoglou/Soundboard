using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Avalonia.Discovery;

public partial class CategoryModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial int SoundCount { get; set; }
    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial IBrush BackgroundBrush { get; set; }

    public bool IsAll { get; init; }

    public CategoryModel(string name, int soundCount, IBrush backgroundBrush, bool isAll = false)
    {
        Name = name;
        SoundCount = soundCount;
        IsEnabled = true;
        BackgroundBrush = backgroundBrush;
        IsAll = isAll;
    }

    /// <summary>
    /// Design-time constructor with sample category data.
    /// </summary>
    public CategoryModel() : this("Basses", 12, ColorPalette.GetBrush(0))
    {
    }
}