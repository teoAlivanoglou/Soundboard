using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Models;
using System.Collections.ObjectModel;
using System.IO;
using Soundboard.AudioEngine;
using Soundboard.Utils;
using Soundboard.Resources;
using Soundboard.Settings;
using Microsoft.Extensions.DependencyInjection;


namespace Soundboard.ViewModels;

public partial class SoundboardViewModel : ObservableObject
{

    private readonly AudioPlaybackEngine _audioEngine;

    public ObservableCollection<Sound> SoundItems { get; set; }
    public ObservableCollection<CategoryFilter> Categories { get; set; }

    // ReSharper disable InconsistentNaming
    [ObservableProperty] public bool settingsVisible = false;
    [ObservableProperty] public bool settingsButtonVisible = false;
    // ReSharper enable InconsistentNaming

    private string lastChosenDirectory = string.Empty;

    [ObservableProperty] public SettingsService _settings;

    public SoundboardViewModel() : this(App.Host.Services.GetRequiredService<AudioPlaybackEngine>(),
        App.Host.Services.GetRequiredService<SettingsService>())
    {
    }

    public SoundboardViewModel(AudioPlaybackEngine audioEngine, SettingsService settings)
    {
        _audioEngine = audioEngine;
        Settings = settings;

        SoundItems =
        [
            new Sound("Please Select a Folder", Assets.ResourceManager.GetStream("Warning"), category: "Debug1"),
            new Sound("Debug Sound", Assets.ResourceManager.GetStream("Warning"), category: "Debug2")
        ];


        Categories = new ObservableCollection<CategoryFilter>(SoundItems.Select(s => new CategoryFilter(s.Category))
            .DistinctBy(c => c.Category).Prepend(new CategoryFilter("All"))); // [new CategoryFilter("All")];
        Categories[1].Enabled = false;


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

    }

    // public bool DirectoryContainsFiles(string path)
    // {
    //     var items = Directory.EnumerateFiles(path);
    //     using var en = items.GetEnumerator();
    //     return !en.MoveNext();
    // }

    // private int group = 0;
    private static readonly string[] validExtensions = [".mp3", ".wav", ".ogg"];

    private static void ReadSoundsRecursive(FileTreeStructure fsStructure, ref List<Sound> tempSounds,
        ref List<CategoryFilter> tempCategories, int maxDepth)
    {
        var category = fsStructure.Depth <= maxDepth
            ? Path.GetRelativePath(fsStructure.RootPath, fsStructure.DirectoryPath)
            : tempCategories[^1].Category;


        var filesAdded = 0;

        foreach (var file in fsStructure.Files)
        {
            string mimeType;
            try
            {
                mimeType = FileUtils.GetMimeFromFile(file.FullName);
            }
            catch (Exception)
            {
                mimeType = validExtensions.Contains(file.Extension) ? "audio" : ".";
            }

            if (!mimeType.StartsWith("audio", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            filesAdded++;

            var sound = new Sound(Path.GetFileNameWithoutExtension(file.Name), file.FullName, category)
            {
                FullCategory = Path.GetRelativePath(fsStructure.RootPath, fsStructure.DirectoryPath)
            };
            sound.Group = Sound.GetGroupIndex(sound);
            tempSounds.Add(sound);
        }

        tempCategories.Add(new CategoryFilter(category, filesAdded));


        foreach (var directory in fsStructure.Directories)
        {
            ReadSoundsRecursive(directory, ref tempSounds, ref tempCategories, maxDepth);
        }
    }


    public void ReadSounds(string filesystemUrl)
    {
        lastChosenDirectory = filesystemUrl;
        if (string.IsNullOrWhiteSpace(lastChosenDirectory)) return;

        Sound.ResetGroups();

        var tempCategories = new List<CategoryFilter>(10) { new("All", 1) };
        var tempSounds = new List<Sound>(16) { };

        var fsStructure = new FileTreeStructure(filesystemUrl);
        ReadSoundsRecursive(fsStructure, ref tempSounds, ref tempCategories,
            Settings.ApplicationUiSettings.MaxFolderLevel);

        tempCategories = tempCategories
            .GroupBy(c => c.Category)
            .Select(g => new CategoryFilter(g.Key, g.Sum(c => c.ChildrenCount)))
            .Where(c => c.ChildrenCount > 0)
            .ToList();

        tempCategories[0].ChildrenCount = tempCategories.Sum(c => c.ChildrenCount) - 1;


        CategoryColors.CategoriesColors.Clear();
        SoundItems.Clear();
        Categories.Clear();
        foreach (var sound in tempSounds)
        {
            SoundItems.Add(sound);
        }


        foreach (var categoryFilter in tempCategories)
        {
            Categories.Add(categoryFilter);
        }
    }


    public void RefreshSounds()
    {
        ReadSounds(lastChosenDirectory);
    }


    public void PlaySound(Sound? sound)
    {
        // var p = CategoryColors.PalettePrimary;
        // var c = CategoryColors.CategoriesColors;
        ArgumentNullException.ThrowIfNull(sound);

        _audioEngine.PlaySound(sound,
            Settings.AudioPlayerSettings.FadeInTime);
    }


    public void StopSound(Sound? sound)
    {
        ArgumentNullException.ThrowIfNull(sound);
        _audioEngine.StopSound(sound);
    }


    public void StopAllSounds()
    {
        _audioEngine.StopAllSounds();
    }


    public void ResetAudioDriver()
    {
        _audioEngine.StopAllSounds();
        _audioEngine.Dispose();
        _audioEngine.Reset();
    }

    public void ToggleSettings()
    {
        SettingsVisible = !SettingsVisible;

        if (!SettingsVisible)
        {
            Settings.Save();
        }
    }
}