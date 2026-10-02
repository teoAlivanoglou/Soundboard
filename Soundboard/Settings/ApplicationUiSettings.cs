using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Settings;

public partial class ApplicationUiSettings : ObservableObject
{
    [ObservableProperty] public partial int MinButtonSize { get; set; } = 70;
    [ObservableProperty] public partial int ButtonGap { get; set; } = 3;
    [ObservableProperty] public partial int MaxFolderLevel { get; set; } = 2;
    [ObservableProperty] public partial int ItemWidth { get; set; } = 70;
    [ObservableProperty] public partial int WindowWidth { get; set; } = 800;
    [ObservableProperty] public partial int WindowHeight { get; set; } = 600;
}