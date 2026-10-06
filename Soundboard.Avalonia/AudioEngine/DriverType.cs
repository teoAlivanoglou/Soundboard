using System.ComponentModel;

namespace Soundboard.Avalonia.AudioEngine;

public enum DriverType
{
    [Description("WaveOut")]
    WaveOutEvent = 0,

    [Description("WASAPI")]
    Wasapi = 1,

    [Description("DirectSound")]
    DirectSound = 2,
}