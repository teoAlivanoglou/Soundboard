using System.Configuration;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Text.Json;
using Soundboard.AudioEngine;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Soundboard.Settings;

public class ApplicationSettingsManager
{
    public AudioPlayerSettings AudioPlayerSettings { get; private set; } = new();
    public ApplicationUiSettings ApplicationUiSettings { get; private set; } = new();

    private string _serializedSettings = string.Empty;

    private ApplicationSettingsManager()
    {

    }

    public static string SettingsPath { get; private set; } = Path.Join(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        Assembly.GetExecutingAssembly().GetName().Name,
        SettingsName);


    private const string SettingsName = "settings.yaml";

    public static ApplicationSettingsManager Settings { get; private set; }

    private static ISerializer Serializer { get; set; } = new SerializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .WithEnumNamingConvention(NullNamingConvention.Instance)
        .WithTypeConverter(new EnumConverter<SampleRate>(SerializedToken.Value, true))
        .WithTypeConverter(new EnumConverter<DriverType>(SerializedToken.Key, true))
        .ConfigureDefaultValuesHandling(DefaultValuesHandling.OmitDefaults)
        .Build();

    private static IDeserializer Deserializer { get; set; } = new DeserializerBuilder()
        .WithNamingConvention(NullNamingConvention.Instance)
        .WithEnumNamingConvention(NullNamingConvention.Instance)
        .WithTypeConverter(new EnumConverter<SampleRate>(SerializedToken.Value, true))
        .WithTypeConverter(new EnumConverter<DriverType>(SerializedToken.Key, true))
        .EnablePrivateConstructors()
        .Build();

    static ApplicationSettingsManager()
    {
        Settings = File.Exists(SettingsPath) ? LoadSettings() : CreateSettings();

    }

    private static ApplicationSettingsManager LoadSettings()
    {
        using var reader = File.OpenText(SettingsPath);
        
        var text = reader.ReadToEnd();
        Settings = Deserializer.Deserialize<ApplicationSettingsManager>(text);
        return Settings;
    }

    private static ApplicationSettingsManager CreateSettings()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath)!);
        Settings = new ApplicationSettingsManager();
        SaveSettings();
        return Settings;
    }

    public static void SaveSettings()
    {
        Settings._serializedSettings = Serializer.Serialize(Settings);
        using var writer = File.CreateText(SettingsPath);
        writer.WriteAsync(Settings._serializedSettings);
    }
}