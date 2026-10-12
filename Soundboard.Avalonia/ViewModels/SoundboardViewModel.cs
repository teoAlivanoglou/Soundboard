using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.TextFormatting;
using Avalonia.Platform;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Soundboard.Avalonia.AudioEngine;
using Soundboard.Avalonia.Discovery;
using Soundboard.Avalonia.Settings;
using Soundboard.Avalonia.Utils;

namespace Soundboard.Avalonia.ViewModels;

public class SoundRow(IReadOnlyList<SoundModel> items)
{
    public IReadOnlyList<SoundModel> Items { get; } = items;
}
public partial class SoundboardViewModel : ObservableObject
{
    private readonly AudioPlaybackEngine? _audioEngine;
    private readonly SoundDiscoveryService _soundDiscoveryService;

    public ObservableCollection<SoundModel> SoundItems { get; set; } = [];
    public ObservableCollection<CategoryModel> Categories { get; set; } = [];
    [ObservableProperty] public partial ObservableCollection<SoundRow> SoundRows { get; set; } = [];

    // ObservableCollection<SoundModel> VisibleSoundItems { get; } = [];
    [ObservableProperty] public partial ObservableCollection<SoundModel> VisibleSoundItems { get; set; } = [];

    [ObservableProperty] public partial double ItemSize { get; set; } = 140;
    [ObservableProperty] public partial Thickness RowMargin { get; set; } = new(0, 0, 0, 8);

    private double _lastGridWidth;
    private int _lastColumnCount;


    [ObservableProperty] public partial bool SettingsVisible { get; set; } = false;
    [ObservableProperty] public partial bool SettingsButtonVisible { get; set; } = false;
    [ObservableProperty] public partial SettingsService Settings { get; set; }

    public DriverType[] AvailableDriverTypes { get; } = [.. AudioPlaybackEngine.GetAvailableDrivers()];
    public SampleRate[] AvailableSampleRates { get; } = Enum.GetValues<SampleRate>();

    private string _lastChosenDirectory = string.Empty;

    /// <summary>
    /// Design-time constructor with sample data for the XAML previewer.
    /// </summary>
    public SoundboardViewModel()
    {
        Settings = new SettingsService();
        SettingsVisible = true;
        _audioEngine = null;
        _soundDiscoveryService = new SoundDiscoveryService();

        var colorIdx = 0;

        // 1. Top-level loose sounds (root)
        var catRoot = new CategoryModel("(root)", 1, ColorPalette.GetColor(colorIdx++)) { RelativePath = "" };

        // 2. Drums: has both direct sounds and subfolder (demonstrating child "(root)")
        var drumsColor = ColorPalette.GetColor(colorIdx++);
        var catDrums = new CategoryModel("Drums", 3, drumsColor) { RelativePath = "Drums" };
        var catDrumsRoot = new CategoryModel("(root)", 2, drumsColor) { Parent = catDrums, RelativePath = "Drums" };
        var catDrumsAcoustic = new CategoryModel("Acoustic", 1, ColorPalette.GetColor(colorIdx++)) { Parent = catDrums, RelativePath = "Drums/Acoustic" };
        catDrums.Children.Add(catDrumsRoot);
        catDrums.Children.Add(catDrumsAcoustic);

        // 3. Synths: folder with only subcategories (0 direct sounds -> uncolored container)
        var catSynths = new CategoryModel("Synths", 3) { RelativePath = "Synths" };
        var catBass = new CategoryModel("Basses", 2, ColorPalette.GetColor(colorIdx++)) { Parent = catSynths, RelativePath = "Synths/Basses" };
        var catLead = new CategoryModel("Leads", 1, ColorPalette.GetColor(colorIdx++)) { Parent = catSynths, RelativePath = "Synths/Leads" };
        catSynths.Children.Add(catBass);
        catSynths.Children.Add(catLead);

        // 4. Consolidated linear chain: "Packs / AZPEX - CROCODILE V2 DRUM KIT" (0 direct sounds -> uncolored container)
        var catCroc = new CategoryModel("AZPEX - CROCODILE V2 DRUM KIT", 1)
        {
            Prefix = "Packs / ",
            RelativePath = "Packs/AZPEX - CROCODILE V2 DRUM KIT"
        };
        var catCrocBasses = new CategoryModel("Basses", 1, ColorPalette.GetColor(colorIdx++))
        {
            Parent = catCroc,
            RelativePath = "Packs/AZPEX - CROCODILE V2 DRUM KIT/Basses"
        };
        catCroc.Children.Add(catCrocBasses);

        // 5. FX: simple leaf category (no subfolders)
        var catFX = new CategoryModel("FX", 1, ColorPalette.GetColor(colorIdx++)) { RelativePath = "FX" };

        Categories.Add(catRoot);
        Categories.Add(catDrums);
        Categories.Add(catSynths);
        Categories.Add(catCroc);
        Categories.Add(catFX);

        SoundItems.Add(new SoundModel("Intro Chime", "", catRoot, "", TimeSpan.FromSeconds(1.2), index: 1));
        SoundItems.Add(new SoundModel("808 Boom", "", catDrumsRoot, "Drums", TimeSpan.FromSeconds(2.0), index: 2));
        SoundItems.Add(new SoundModel("Punchy Clap", "", catDrumsRoot, "Drums", TimeSpan.FromMilliseconds(450), index: 3));
        SoundItems.Add(new SoundModel("Snappy Rimshot", "", catDrumsAcoustic, "Drums/Acoustic", TimeSpan.FromMilliseconds(120), index: 4));
        SoundItems.Add(new SoundModel("Cleanest 808", "", catBass, "Synths/Basses", TimeSpan.FromSeconds(2.4), index: 5)
        {
            IsPlaying = true,
            Progress = 0.35
        });
        SoundItems.Add(new SoundModel("Deep Sub Bass", "", catBass, "Synths/Basses", TimeSpan.FromSeconds(3.8), index: 6));
        SoundItems.Add(new SoundModel("Hyper Synth Lead", "", catLead, "Synths/Leads", TimeSpan.FromMilliseconds(850), index: 7));
        SoundItems.Add(new SoundModel("AZPEX [ BASS ] ARCH", "", catCrocBasses, "Packs/AZPEX - CROCODILE V2 DRUM KIT/Basses", TimeSpan.FromSeconds(2.1), index: 8));
        SoundItems.Add(new SoundModel("Laser Zap", "", catFX, "FX", TimeSpan.FromMilliseconds(620), index: 9));
        // UpdateLayoutAndRows(800);
        UpdateVisibleSounds();
        HookThemeChanges();
    }

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

        Settings.ApplicationUiSettings.PropertyChanged += (_, arg) => {
            if (arg.PropertyName
                is nameof(ApplicationUiSettings.MinButtonSize)
                or nameof(ApplicationUiSettings.ButtonGap))
            {
                // UpdateLayoutAndRows(forceRebuild: true);
                UpdateVisibleSounds();
            }
        };

        Settings.AudioPlayerSettings.PropertyChanged += (_, arg) => {
            if (arg.PropertyName is nameof(AudioPlayerSettings.Volume))
            {
                _audioEngine?.SetVolume(Settings.AudioPlayerSettings.Volume);
            }
        };

        HookThemeChanges();
        LoadDefaultSounds();
    }

    private void HookThemeChanges()
    {
        if (Application.Current is not null)
        {
            Application.Current.ActualThemeVariantChanged += (_, _) =>
            {
                foreach (var category in Categories)
                {
                    category.NotifyThemeChanged();
                }
            };
        }
    }

    public void UpdateVisibleSounds()
    {
        var filtered = SoundItems.Where(s => s.Category.IsEnabled).ToList();
        VisibleSoundItems = new ObservableCollection<SoundModel>(filtered);
    }


    public void UpdateLayoutAndRows(double? availableWidth = null, bool forceRebuild = false)
    {
        if (availableWidth is > 0)
        {
            _lastGridWidth = availableWidth.Value;
        }

        if (_lastGridWidth <= 0) return;

        var minSize = Math.Max(1.0, Settings.ApplicationUiSettings.MinButtonSize);
        var gap = Math.Max(0.0, Settings.ApplicationUiSettings.ButtonGap);

        RowMargin = new Thickness(0, 0, 0, gap);

        // Column and item size (Math.Floor prevents sub-pixel rounding overflow on right edge)
        var columns = Math.Max(1, (int)((_lastGridWidth + gap) / (minSize + gap)));
        var itemSize = Math.Max(1.0, Math.Floor((_lastGridWidth - (columns - 1) * gap) / columns));

        if (Math.Abs(ItemSize - itemSize) >= 0.5)
        {
            ItemSize = itemSize;
        }

        // If columns didn't change and we're not forcing a rebuild, the rows are already correct!
        // We only needed to update ItemSize (which lets existing pads resize smoothly with 0 allocations).
        if (columns == _lastColumnCount && !forceRebuild && SoundRows.Count > 0)
        {
            return;
        }

        _lastColumnCount = columns;

        // Filter visible sounds and chunk into rows
        var visibleSounds = SoundItems.Where(s => s.Category.IsEnabled).ToList();
        var newRows = visibleSounds.Chunk(columns)
            .Select(chunk => new SoundRow(chunk))
            .ToList();

        // Assigning SoundRows automatically fires 1 PropertyChanged notification via source generator
        SoundRows = new ObservableCollection<SoundRow>(newRows);
    }

    public void LoadDefaultSounds()
    {
        var uri = new Uri("avares://Soundboard.Avalonia/Assets/warning.mp3");

        if (!AssetLoader.Exists(uri)) return;

        var stream = AssetLoader.Open(uri);

        var defaultCategory = new CategoryModel("Info", 1, ColorPalette.GetColor(0));
        var warningSound = SoundModel.FromStream("Please Select a Folder", stream, defaultCategory, index: 1);

        Categories.Clear();
        Categories.Add(defaultCategory);

        SoundItems.Clear();
        SoundItems.Add(warningSound);

        // UpdateLayoutAndRows(forceRebuild: true);
        UpdateVisibleSounds();
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

        // UpdateLayoutAndRows(forceRebuild: true);
        UpdateVisibleSounds();
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

        // UpdateLayoutAndRows(forceRebuild: true);
        UpdateVisibleSounds();
    }


    public Task RefreshSoundsAsync() => ReadSoundsAsync(_lastChosenDirectory);

    public void UpdateSoundVisibility()
    {
        foreach (var sound in SoundItems)
        {
            sound.IsVisible = sound.Category.IsEnabled;
        }

        // UpdateLayoutAndRows(forceRebuild: true);
        UpdateVisibleSounds();
    }

    [RelayCommand]
    public void PlaySound(SoundModel? sound)
    {
        ArgumentNullException.ThrowIfNull(sound);

        _audioEngine?.PlaySound(sound, Settings.AudioPlayerSettings.FadeInTime);
    }

    [RelayCommand]
    public void StopSound(SoundModel? sound)
    {
        ArgumentNullException.ThrowIfNull(sound);
        _audioEngine?.StopSound(sound);
    }

    [RelayCommand]
    public void ToggleSound(SoundModel? sound)
    {
        if (sound is null) return;
        if (sound.IsPlaying)
            StopSound(sound);
        else
            PlaySound(sound);
    }

    [RelayCommand]
    public void StopAllSounds() => _audioEngine?.StopAllSounds();

    [RelayCommand]
    public void ToggleSettings()
    {
        SettingsVisible = !SettingsVisible;
        if (!SettingsVisible) Settings.Save();
    }

    [RelayCommand]
    public void ResetAudioDriver()
    {
        _audioEngine?.Reset();
        Settings.Save();
    }

    private double _previousVolume = 70.0;

    [RelayCommand]
    public void ToggleMute()
    {
        if (Settings.AudioPlayerSettings.Volume > 0)
        {
            _previousVolume = Settings.AudioPlayerSettings.Volume;
            Settings.AudioPlayerSettings.Volume = 0;
        }
        else
        {
            Settings.AudioPlayerSettings.Volume = _previousVolume > 0 ? _previousVolume : 70.0;
        }
    }
}