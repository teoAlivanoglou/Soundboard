using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Avalonia.Settings;

public partial class ApplicationUiSettings : ObservableObject
{
    [ObservableProperty] public partial int MinButtonSize { get; set; } = 140;
    [ObservableProperty] public partial int ButtonGap { get; set; } = 10;
    [ObservableProperty] public partial int MaxFolderLevel { get; set; } = 6;
    [ObservableProperty] public partial int ItemWidth { get; set; } = 140;
    [ObservableProperty] public partial int WindowWidth { get; set; } = 800;
    [ObservableProperty] public partial int WindowHeight { get; set; } = 600;
}