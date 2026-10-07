using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Avalonia.AudioEngine;

namespace Soundboard.Avalonia.Settings;

public partial class AudioPlayerSettings : ObservableObject
{
    [ObservableProperty] public partial SampleRate SampleRate { get; set; } = SampleRate.R48000;
    [ObservableProperty] public partial DriverType DriverType { get; set; } = (DriverType)0;
    [ObservableProperty] public partial double Latency { get; set; } = 20;
    [ObservableProperty] public partial int FadeInTime { get; set; } = 200;
    [ObservableProperty] public partial double Volume { get; set; } = 70.0;
    [ObservableProperty] public partial bool Show { get; set; } = true;
}