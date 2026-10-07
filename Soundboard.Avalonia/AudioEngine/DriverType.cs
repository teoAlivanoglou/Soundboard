using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Soundboard.Avalonia.AudioEngine;

[JsonConverter(typeof(JsonStringEnumConverter<DriverType>))]
public enum DriverType
{
    [Description("WaveOut")]
    [JsonStringEnumMemberName("WaveOut")]
    WaveOutEvent = 0,

    [Description("WASAPI")]
    [JsonStringEnumMemberName("WASAPI")]
    Wasapi = 1,

    [Description("DirectSound")]
    [JsonStringEnumMemberName("DirectSound")]
    DirectSound = 2,
}