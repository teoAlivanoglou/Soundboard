using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Soundboard.Avalonia.AudioEngine;

[JsonConverter(typeof(JsonStringEnumConverter<DriverType>))]
public enum DriverType
{
#if WINDOWS
    [Description("WASAPI")]
    [JsonStringEnumMemberName("WASAPI")]
    Wasapi = 0,

    [Description("WaveOut")]
    [JsonStringEnumMemberName("WaveOut")]
    WaveOutEvent = 1,

    [Description("DirectSound")]
    [JsonStringEnumMemberName("DirectSound")]
    DirectSound = 2,
#else
    [Description("CoreAudio")]
    [JsonStringEnumMemberName("CoreAudio")]
    CoreAudio = 0,
#endif
}
