using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Settings;

public partial class ApplicationSettings : ObservableObject
{
    // ReSharper disable InconsistentNaming
    [ObservableProperty] public int minButtonSize = 70;
    [ObservableProperty] public int buttonGap = 3;
    [ObservableProperty] public int fadeInTime = 200;
    [ObservableProperty] public int maxFolderLevel = 2;
    [ObservableProperty] public int itemWidth = 70;
    // ReSharper enable InconsistentNaming
}