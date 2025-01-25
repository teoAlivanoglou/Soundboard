using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Settings;

public partial class ApplicationUiSettings : ObservableObject
{
    // ReSharper disable InconsistentNaming
    [ObservableProperty] private int minButtonSize = 70;
    [ObservableProperty] private int buttonGap = 3;
    [ObservableProperty] private int maxFolderLevel = 2;
    [ObservableProperty] private int itemWidth = 70;
    // ReSharper enable InconsistentNaming
}