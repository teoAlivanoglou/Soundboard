using System.ComponentModel;

namespace Soundboard.Avalonia.AudioEngine;

public enum SampleRate
{
    [Description("44100")] R44100 = 44100,
    [Description("48000")] R48000 = 48000,
    [Description("88200")] R88200 = 88200,
    [Description("96000")] R96000 = 96000,
    [Description("192000")] R192000 = 192000
}