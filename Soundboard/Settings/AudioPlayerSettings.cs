using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.AudioEngine;

namespace Soundboard.Settings;

public partial class AudioPlayerSettings : ObservableObject
{
    [ObservableProperty] public partial SampleRate SampleRate { get; set; } = SampleRate.R48000;
    [ObservableProperty] public partial DriverType DriverType { get; set; } = DriverType.Wasapi;
    [ObservableProperty] public partial double Latency { get; set; } = 20;
    [ObservableProperty] public partial int FadeInTime { get; set; } = 200;
    [ObservableProperty] public partial bool Show { get; set; } = true;
}