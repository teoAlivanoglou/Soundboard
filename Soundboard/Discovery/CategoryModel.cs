using CommunityToolkit.Mvvm.ComponentModel;
using System.Windows.Media;

namespace Soundboard.Discovery;

public partial class CategoryModel : ObservableObject
{
    [ObservableProperty] public partial string Name { get; set; }
    [ObservableProperty] public partial int SoundCount { get; set; }
    [ObservableProperty] public partial bool IsEnabled { get; set; }
    [ObservableProperty] public partial Brush BackgroundBrush { get; set; }

    public bool IsAll { get; init; }

    public CategoryModel(string name, int soundCount, Brush backgroundBrush, bool isAll = false)
    {
        Name = name;
        SoundCount = soundCount;
        IsEnabled = true;
        BackgroundBrush = backgroundBrush;
        IsAll = isAll;
    }
}