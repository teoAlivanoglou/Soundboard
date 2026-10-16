using System.Text.Json.Serialization;
using SonicPad.AudioEngine;

namespace SonicPad.Settings;

[JsonSourceGenerationOptions(
    WriteIndented = true,
    DefaultIgnoreCondition = JsonIgnoreCondition.Never)]
[JsonSerializable(typeof(SettingsService))]
[JsonSerializable(typeof(AudioPlayerSettings))]
[JsonSerializable(typeof(ApplicationUiSettings))]
[JsonSerializable(typeof(DriverType))]
[JsonSerializable(typeof(SampleRate))]
internal partial class SettingsJsonContext : JsonSerializerContext
{
}
