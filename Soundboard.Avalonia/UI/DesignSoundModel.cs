using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Discovery;

namespace Soundboard.Avalonia.UI;

public class DesignSoundModel
{
    public string Name { get; set; } = "Cleanest 808";
    public string Duration { get; set; } = "0:04";
    public string PadNumber { get; set; } = "#01";
    public bool IsPlaying { get; set; } = true;
    public double Progress { get; set; } = 0.05;

    public DesignCategoryModel Category { get; set; } = new();
}

public class DesignCategoryModel
{
    public string Name { get; set; } = "Bass";
    public int SoundCount { get; set; } = 12;
    public bool IsEnabled { get; set; } = true;
    public SolidColorBrush BackgroundBrush { get; set; } = new (Color.FromRgb(0xF8, 0xBD, 0x28));

    public bool IsAll { get; init; } = false;

}