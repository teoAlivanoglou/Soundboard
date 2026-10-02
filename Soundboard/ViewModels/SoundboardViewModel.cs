using CommunityToolkit.Mvvm.ComponentModel;
using System.Collections.ObjectModel;
using System.IO;
using Soundboard.AudioEngine;
using Soundboard.Utils;
using Soundboard.Resources;
using Soundboard.Settings;
using Microsoft.Extensions.DependencyInjection;
using Soundboard.Discovery;
using System.Web;


namespace Soundboard.ViewModels;

public partial class SoundboardViewModel : ObservableObject
{
    private readonly AudioPlaybackEngine _audioEngine;
    private readonly SoundDiscoveryService _soundDiscoveryService;

    public ObservableCollection<SoundModel> SoundItems { get; set; } = [];
    public ObservableCollection<CategoryModel> Categories { get; set; } = [];

    [ObservableProperty] public partial bool SettingsVisible { get; set; } = false;
    [ObservableProperty] public partial bool SettingsButtonVisible { get; set; } = false;
    [ObservableProperty] public partial SettingsService Settings { get; set; }

    private string _lastChosenDirectory = string.Empty;

    public SoundboardViewModel(AudioPlaybackEngine audioEngine, SoundDiscoveryService soundDiscoveryService,
        SettingsService settings)
    {
        _audioEngine = audioEngine;
        _soundDiscoveryService = soundDiscoveryService;
        Settings = settings;

        var args = Environment.GetCommandLineArgs();
        var options = Options.Parse(args);

        if (options?.OutputType != null)
        {
            Settings.AudioPlayerSettings.DriverType = options.OutputType.Value;
        }

        if (options?.Debug is true)
        {
            SettingsButtonVisible = true;
            Settings.SettingsPanelVisible = true;
        }

        LoadDefaultSounds();
    }

    public void LoadDefaultSounds()
    {
        var stream = Assets.ResourceManager.GetStream("Warning");
        if (stream is null) return;

        var defaultCategory = new CategoryModel("Info", 1, ColorPalette.GetBrush(0));
        var warningSound = SoundModel.FromStream("Please Select a Folder", stream, defaultCategory);

        Categories.Clear();
        Categories.Add(new CategoryModel("All", 1, ColorPalette.AllButtonBrush, isAll: true));
        Categories.Add(defaultCategory);

        SoundItems.Clear();
        SoundItems.Add(warningSound);
    }

    public void ReadSounds(string filesystemUrl)
    {
        _lastChosenDirectory = filesystemUrl;
        if (string.IsNullOrWhiteSpace(_lastChosenDirectory)) return;

        var result = _soundDiscoveryService.DiscoverSounds(
            filesystemUrl,
            Settings.ApplicationUiSettings.MaxFolderLevel);

        if (result.Sounds.Count == 0)
        {
            LoadDefaultSounds();
            return;
        }

        Categories.Clear();
        foreach (var category in result.Categories)
        {
            Categories.Add(category);
        }

        SoundItems.Clear();
        foreach (var sound in result.Sounds)
        {
            SoundItems.Add(sound);
        }
    }

    public async Task ReadSoundsAsync(string filesystemUrl)
    {
        _lastChosenDirectory = filesystemUrl;
        if (string.IsNullOrWhiteSpace(_lastChosenDirectory)) return;

        var result = await _soundDiscoveryService.DiscoverSoundsAsync(
            filesystemUrl,
            Settings.ApplicationUiSettings.MaxFolderLevel);

        if (result.Sounds.Count == 0)
        {
            LoadDefaultSounds();
            return;
        }

        Categories.Clear();
        foreach (var category in result.Categories)
        {
            Categories.Add(category);
        }

        SoundItems.Clear();
        foreach (var sound in result.Sounds)
        {
            SoundItems.Add(sound);
        }
    }


    public Task RefreshSoundsAsync() => ReadSoundsAsync(_lastChosenDirectory);

    public void UpdateSoundVisibility()
    {
        foreach (var sound in SoundItems)
        {
            sound.IsVisible = sound.Category.IsEnabled;
        }
    }

    public void PlaySound(SoundModel? sound)
    {
        ArgumentNullException.ThrowIfNull(sound);

        _audioEngine.PlaySound(sound, Settings.AudioPlayerSettings.FadeInTime);
    }


    public void StopSound(SoundModel? sound)
    {
        ArgumentNullException.ThrowIfNull(sound);
        _audioEngine.StopSound(sound);
    }


    public void StopAllSounds() => _audioEngine.StopAllSounds();

    public void ToggleSettings()
    {
        SettingsVisible = !SettingsVisible;
        if (!SettingsVisible) Settings.Save();
    }

    public void ResetAudioDriver()
    {
        _audioEngine.Reset();
    }
}