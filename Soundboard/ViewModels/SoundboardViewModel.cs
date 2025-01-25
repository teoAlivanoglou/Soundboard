using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Models;
using System.Collections.ObjectModel;
using System.IO;
using Soundboard.AudioEngine;
using Soundboard.Utils;
using Soundboard.Resources;
using Soundboard.Settings;

// ReSharper disable InconsistentNaming

namespace Soundboard.ViewModels;

public partial class SoundboardViewModel : ObservableObject
{
    public ObservableCollection<Sound> SoundItems { get; set; }
    public ObservableCollection<CategoryFilter> Categories { get; set; }

    // [ObservableProperty] public int columns = 4;
    [ObservableProperty] public bool settingsVisible = false;
    [ObservableProperty] public bool settingsButtonVisible = false;

    private string lastChosenDirectory = string.Empty;

    // [ObservableProperty] public AudioPlayerSettings audioPlayerSettings = ApplicationSettingsManager.Settings.AudioPlayerSettings;
    // [ObservableProperty] public ApplicationUiSettings applicationUiSettings = ApplicationSettingsManager.Settings.ApplicationUiSettings;
    // ReSharper enable InconsistentNaming


    public SoundboardViewModel()
    {
        SoundItems =
        [
            new Sound("Please Select a Folder", Assets.ResourceManager.GetStream("Warning")),
        ];

        Categories = [new CategoryFilter("All")];

        var args = Environment.GetCommandLineArgs();
        var options = Options.Parse(args);

        if (options?.OutputType != null)
        {
            ApplicationSettingsManager.Settings.AudioPlayerSettings.DriverType = options.OutputType.Value;
        }

        if (options?.Debug is true)
        {
            SettingsButtonVisible = true;
        }

        AudioPlaybackEngine.Initialize();
    }

    public bool DirectoryContainsFiles(string path)
    {
        var items = Directory.EnumerateFiles(path);
        using var en = items.GetEnumerator();
        return !en.MoveNext();
    }

    // public void ReadSoundsRecursive(string path, ref List<CategoryFilter> categories, ref List<Sound> sounds, int depth,
    //     string categoryPrefix)
    // {
    //     var category = Path.GetFileName(path);
    //
    //     if (!string.IsNullOrWhiteSpace(categoryPrefix))
    //     {
    //         category = Path.Combine(categoryPrefix, category);
    //     }
    //
    //     categories.Add(new CategoryFilter(category + depth));
    //
    //     foreach (var file in Directory.EnumerateFiles(path))
    //     {
    //         if (!FileUtils.GetMimeFromFile(file).StartsWith("audio", StringComparison.OrdinalIgnoreCase)) continue;
    //
    //         var name = Path.GetFileNameWithoutExtension(file);
    //         sounds.Add(new Sound(name, file, category) { topLevel = true });
    //     }
    //
    //     foreach (var directory in Directory.EnumerateDirectories(path))
    //     {
    //         if (DirectoryContainsFiles(directory))
    //         {
    //             ReadSoundsRecursive(directory, ref categories, ref sounds, depth + 1, string.Empty);
    //         }
    //         else
    //         {
    //             ReadSoundsRecursive(directory, ref categories, ref sounds, depth + 1, category);
    //         }
    //     }
    // }


    private int group = 0;
    private static string[] validExtensions = new[] { ".mp3", ".wav", ".ogg" };

    private void ReadSoundsRecursive2(FileTreeStructure fsStructure, ref List<Sound> tempSounds,
        ref List<CategoryFilter> tempCategories, int maxDepth)
    {
        string category;
        if (fsStructure.Depth <= maxDepth)
        {
            category = Path.GetRelativePath(fsStructure.RootPath, fsStructure.DirectoryPath);
            group = Sound.GetGroupIndex(fsStructure.DirectoryPath, fsStructure.Files.Count > 0);
        }
        else
        {
            category = tempCategories[^1].Category;
            group = Sound.GetGroupIndex(fsStructure.DirectoryPath, fsStructure.Files.Count > 0);
        }

        tempCategories.Add(new CategoryFilter(category, fsStructure.Files.Count));

        foreach (var file in fsStructure.Files)
        {
            string mimeType;
            try
            {
                mimeType = FileUtils.GetMimeFromFile(file.FullName);
            }
            catch (Exception e)
            {
                mimeType = validExtensions.Contains(file.Extension) ? "audio" : ".";
            }
            if (!mimeType.StartsWith("audio", StringComparison.OrdinalIgnoreCase)) continue;
            tempSounds.Add(
                new Sound(Path.GetFileNameWithoutExtension(file.Name), file.FullName, category, group: group));
        }

        foreach (var directory in fsStructure.Directories)
        {
            ReadSoundsRecursive2(directory, ref tempSounds, ref tempCategories, maxDepth);
        }
    }


    public void ReadSounds(string filesystemUrl)
    {
        lastChosenDirectory = filesystemUrl;
        if (string.IsNullOrWhiteSpace(lastChosenDirectory)) return;

        Sound.ResetGroups();

        var tempCategories = new List<CategoryFilter>(10) { new("All", 1) };
        if (tempCategories == null) throw new ArgumentNullException(nameof(tempCategories));

        var tempSounds = new List<Sound>(16) { };
        if (tempSounds == null) throw new ArgumentNullException(nameof(tempSounds));

        var fsStructure = new FileTreeStructure(filesystemUrl);
        ReadSoundsRecursive2(fsStructure, ref tempSounds, ref tempCategories, ApplicationSettingsManager.Settings.ApplicationUiSettings.MaxFolderLevel);

        tempCategories = tempCategories
            .GroupBy(c => c.Category)
            .Select(g => new CategoryFilter(g.Key, g.Sum(c => c.ChildrenCount)))
            .Where(c => c.ChildrenCount > 0)
            .ToList();

        tempCategories[0].ChildrenCount = tempCategories.Sum(c => c.ChildrenCount) - 1;

        // TODO: FUCKING FIX THIS COLOR SHIT OR I WILL LOSE MY SHIT

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
        if (sound is null)
            throw new ArgumentNullException(nameof(sound));
        AudioPlaybackEngine.Instance.PlaySound(sound, ApplicationSettingsManager.Settings.AudioPlayerSettings.FadeInTime);
    }


    public void StopSound(Sound? sound)
    {
        if (sound is null)
            throw new ArgumentNullException(nameof(sound));
        AudioPlaybackEngine.Instance.StopSound(sound);
    }

    public void StopAllSounds()
    {
        AudioPlaybackEngine.Instance.StopAllSounds();
    }




    public void ResetAudioDriver()
    {
        AudioPlaybackEngine.Instance.StopAllSounds();
        AudioPlaybackEngine.Instance.Dispose();
        AudioPlaybackEngine.Initialize();
    }
}