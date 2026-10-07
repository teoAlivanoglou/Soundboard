using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Avalonia.AudioEngine;

namespace Soundboard.Avalonia.Settings;

public partial class SettingsService : ObservableObject
{
    public AudioPlayerSettings AudioPlayerSettings { get; set; } = new();
    public ApplicationUiSettings ApplicationUiSettings { get; set; } = new();

    [JsonIgnore]
    [ObservableProperty] public partial bool SettingsPanelVisible { get; set; } = false;

    public static string SettingsPath =
        Path.Join(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            Assembly.GetExecutingAssembly().GetName().Name,
            SettingsName);

    private const string SettingsName = "settings.json";

    public static SettingsService Load()
    {
        // If file doesn't exist, create default settings and save them
        if (!File.Exists(SettingsPath))
        {
            var defaults = new SettingsService();
            defaults.Save();
            return defaults;
        }

        // If file exists, try loading it
        try
        {
            using var stream = File.OpenRead(SettingsPath);
            var loaded = JsonSerializer.Deserialize<SettingsService>(stream);
            return loaded ?? new SettingsService();
        }
        catch
        {
            // Fallback if JSON is corrupted
            return new SettingsService();
        }
    }


    public void Save()
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);

            if (directory != null)
                Directory.CreateDirectory(directory);

            var options = new JsonSerializerOptions { WriteIndented = true };
            var tempPath = SettingsPath + ".tmp";
            using (var stream = File.Create(tempPath))
            {
                JsonSerializer.Serialize(stream, this, options);
            }

            File.Move(tempPath, SettingsPath, overwrite: true);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[SettingsService] Save failed: {ex}");
        }
    }
    //
    // private static ISerializer Serializer { get; } = new SerializerBuilder()
    //     .WithNamingConvention(NullNamingConvention.Instance)
    //     .WithEnumNamingConvention(NullNamingConvention.Instance)
    //     .WithTypeConverter(new EnumConverter<SampleRate>(SerializedToken.Value, true))
    //     .WithTypeConverter(new EnumConverter<DriverType>(SerializedToken.Key, true))
    //     .IgnoreFields()
    //     .WithTypeInspector(inner => new Utils.IgnoreNamedPropertyInspector(inner, [nameof(SettingsPanelVisible)],
    //         StringComparison.Ordinal))
    //     .Build();
    //
    // private static IDeserializer Deserializer { get; } = new DeserializerBuilder()
    //     .WithNamingConvention(NullNamingConvention.Instance)
    //     .WithEnumNamingConvention(NullNamingConvention.Instance)
    //     .WithTypeConverter(new EnumConverter<SampleRate>(SerializedToken.Value, true))
    //     .WithTypeConverter(new EnumConverter<DriverType>(SerializedToken.Key, true))
    //     .EnablePrivateConstructors()
    //     .Build();
}