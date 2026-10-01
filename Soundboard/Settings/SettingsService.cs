using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.AudioEngine;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Reflection;
using System.Text;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace Soundboard.Settings
{
    public partial class SettingsService : ObservableObject
    {
        public AudioPlayerSettings AudioPlayerSettings { get; private set; } = new();
        public ApplicationUiSettings ApplicationUiSettings { get; private set; } = new();

        [ObservableProperty] public partial bool SettingsPanelVisible { get; set; } = false;

        private string _serializedSettings = string.Empty;

        public static string SettingsPath =
            Path.Join(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Assembly.GetExecutingAssembly().GetName().Name,
                SettingsName);

        private const string SettingsName = "settings.yaml";


        private SettingsService()
        {
        }

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
                var text = File.ReadAllText(SettingsPath);
                var loaded = Deserializer.Deserialize<SettingsService>(text);
                return loaded ?? new SettingsService();
            }
            catch
            {
                // Fallback if YAML is corrupted
                return new SettingsService();
            }

        }


        public void Save()
        {
            var directory = Path.GetDirectoryName(SettingsPath);

            if (directory != null) 
                Directory.CreateDirectory(directory);

            var yaml = Serializer.Serialize(this);
            File.WriteAllText(SettingsPath, yaml);
        }

        private static ISerializer Serializer { get; set; } = new SerializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .WithEnumNamingConvention(NullNamingConvention.Instance)
            .WithTypeConverter(new EnumConverter<SampleRate>(SerializedToken.Value, true))
            .WithTypeConverter(new EnumConverter<DriverType>(SerializedToken.Key, true))
            .IgnoreFields()
            .WithTypeInspector(inner => new Utils.IgnoreNamedPropertyInspector(inner, [nameof(SettingsPanelVisible)],
                StringComparison.Ordinal))
            .Build();

        private static IDeserializer Deserializer { get; set; } = new DeserializerBuilder()
            .WithNamingConvention(NullNamingConvention.Instance)
            .WithEnumNamingConvention(NullNamingConvention.Instance)
            .WithTypeConverter(new EnumConverter<SampleRate>(SerializedToken.Value, true))
            .WithTypeConverter(new EnumConverter<DriverType>(SerializedToken.Key, true))
            .EnablePrivateConstructors()
            .Build();
    }
}
