using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Media;
using System.Resources;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Soundboard.AudioEngine;
using Soundboard.Utils;
using Microsoft.VisualBasic.Devices;

// ReSharper disable InconsistentNaming

namespace Soundboard.ViewModels;

public partial class SoundboardViewModel : ObservableObject
{
    public ObservableCollection<Sound> SoundItems { get; set; }
    public ObservableCollection<CategoryFilter> Categories { get; set; }
    [ObservableProperty] public int columns = 4;
    [ObservableProperty] public int minButtonSize = 70;
    [ObservableProperty] public int buttonGap = 3;
    [ObservableProperty] public bool settingsVisible = false;
    [ObservableProperty] public int fadeInTime = 200;
    [ObservableProperty] public int maxFolderLevel = 0;


    public AudioPlayer audioPlayer { get; private set; }
    [ObservableProperty] public int itemWidth = 70;

    private string lastChosenDirectory = string.Empty;

    public SoundboardViewModel()
    {
        if (!File.Exists(Path.Combine("SoundboardResources", "warning.mp3")))
        {
            var bytes = Resources.ResourceManager.GetObject("Warning");
            Directory.CreateDirectory("SoundboardResources");
            File.WriteAllBytesAsync(Path.Combine("SoundboardResources", "warning.mp3"), (byte[])bytes!);
        }

        SoundItems =
        [
            new Sound("Please Select a Folder", Path.Combine("SoundboardResources", "warning.mp3")),
        ];

        Categories = [new CategoryFilter("All")];

        audioPlayer = new AudioPlayer();
    }

    public bool DirectoryContainsFiles(string path)
    {
        var items = Directory.EnumerateFiles(path);
        using var en = items.GetEnumerator();
        return !en.MoveNext();
    }

    public void ReadSoundsRecursive(string path, ref List<CategoryFilter> categories, ref List<Sound> sounds, int depth,
        string categoryPrefix)
    {
        var category = Path.GetFileName(path);

        if (!string.IsNullOrWhiteSpace(categoryPrefix))
        {
            category = Path.Combine(categoryPrefix, category);
        }

        categories.Add(new CategoryFilter(category + depth));

        foreach (var file in Directory.EnumerateFiles(path))
        {
            if (!FileUtils.GetMimeFromFile(file).StartsWith("audio", StringComparison.OrdinalIgnoreCase)) continue;

            var name = Path.GetFileNameWithoutExtension(file);
            sounds.Add(new Sound(name, file, category) { topLevel = true });
        }

        foreach (var directory in Directory.EnumerateDirectories(path))
        {
            if (DirectoryContainsFiles(directory))
            {
                ReadSoundsRecursive(directory, ref categories, ref sounds, depth + 1, string.Empty);
            }
            else
            {
                ReadSoundsRecursive(directory, ref categories, ref sounds, depth + 1, category);
            }
        }
    }

    private void ReadSoundsRecursive2(FileTreeStructure fsStructure, ref List<Sound> tempSounds,
        ref List<CategoryFilter> tempCategories, int maxDepth)
    {
        var category = fsStructure.Depth <= maxDepth
            ? Path.GetRelativePath(fsStructure.RootPath, fsStructure.DirectoryPath)
            : tempCategories[^1].Category;

        tempCategories.Add(new CategoryFilter(category));

        foreach (var file in fsStructure.Files)
        {
            if (!FileUtils.GetMimeFromFile(file.FullName)
                    .StartsWith("audio", StringComparison.OrdinalIgnoreCase)) continue;
            tempSounds.Add(new Sound(Path.GetFileNameWithoutExtension(file.Name), file.FullName, category));
        }

        foreach (var directory in fsStructure.Directories)
        {
            ReadSoundsRecursive2(directory, ref tempSounds, ref tempCategories, maxDepth);
        }
    }


    public void ReadSounds(string filesystemUrl)
    {
        lastChosenDirectory = filesystemUrl;

        Sound.ResetGroups();

        var tempCategories = new List<CategoryFilter>(10) { new("All") };
        if (tempCategories == null) throw new ArgumentNullException(nameof(tempCategories));

        var tempSounds = new List<Sound>(16) { };
        if (tempSounds == null) throw new ArgumentNullException(nameof(tempSounds));

        // TODO: Maybe have a 'levels' setting to determine how deep to go into the file tree
        int depth = 0;

        var fsStructure = new FileTreeStructure(filesystemUrl);
        ReadSoundsRecursive2(fsStructure, ref tempSounds, ref tempCategories, 2);


        var category = Path.GetFileName(filesystemUrl);
        tempCategories.Add(new CategoryFilter(category));

        foreach (var file in Directory.EnumerateFiles(filesystemUrl))
        {
            if (!FileUtils.GetMimeFromFile(file).StartsWith("audio", StringComparison.OrdinalIgnoreCase)) continue;

            var name = Path.GetFileNameWithoutExtension(file);
            tempSounds.Add(new Sound(name, file, category) { topLevel = true });
        }

        depth++;
        foreach (var directory in Directory.EnumerateDirectories(filesystemUrl))
        {
            category = Path.GetFileName(directory);
        }


        //
        //
        //
        //
        // foreach (var file in Directory.EnumerateFiles(filesystemUrl, "*", SearchOption.AllDirectories))
        // {
        //     if (!FileUtils.GetMimeFromFile(file).StartsWith("audio")) continue;
        //
        //     var name = Path.GetFileNameWithoutExtension(file);
        //     var category = Path.GetDirectoryName(Path.GetRelativePath(filesystemUrl, file));
        //
        //     var topLevelSound = false;
        //
        //     if (string.IsNullOrWhiteSpace(category))
        //     {
        //         category = Path.GetFileName(filesystemUrl);
        //         topLevelSound = true;
        //     }
        //
        //     tempSounds.Add(new Sound(name, file, category) { topLevel = topLevelSound });
        //     tempCategories.Add(new CategoryFilter(category));
        // }
        //
        // CategoryColors.CategoriesColors.Clear();
        // SoundItems.Clear();
        // Categories.Clear();
        //
        //
        // // BUG: Handle folder with only subfolders... it's fucked
        // // V E R Y   I M P O R T A N T
        //
        //
        // foreach (var sound in tempSounds.OrderByDescending(s => s.topLevel).ThenBy(s => s.Category))
        // {
        //     sound.Categorize();
        //
        //     SoundItems.Add(sound);
        // }
        //
        //
        // foreach (var categoryFilter in tempCategories.DistinctBy(c => c.Category.Split(Path.DirectorySeparatorChar)[0]))
        // {
        //     Categories.Add(categoryFilter);
        // }
    }


    public void RefreshSounds()
    {
        ReadSounds(lastChosenDirectory);
        return;
        Sound.ResetGroups();

        var tempCategories = new List<CategoryFilter>(10) { new("All") };
        if (tempCategories == null) throw new ArgumentNullException(nameof(tempCategories));

        var tempSounds = new List<Sound>(16) { };
        if (tempSounds == null) throw new ArgumentNullException(nameof(tempSounds));

        // TODO: Maybe have a 'levels' setting to determine how deep to go into the file tree

        foreach (var file in Directory.EnumerateFiles(lastChosenDirectory, "*", SearchOption.AllDirectories))
        {
            if (!FileUtils.GetMimeFromFile(file).StartsWith("audio")) continue;

            var name = Path.GetFileNameWithoutExtension(file);
            var category = Path.GetDirectoryName(Path.GetRelativePath(lastChosenDirectory, file));

            var topLevelSound = false;

            if (string.IsNullOrWhiteSpace(category))
            {
                category = Path.GetFileName(lastChosenDirectory);
                topLevelSound = true;
            }

            tempSounds.Add(new Sound(name, file, category) { topLevel = topLevelSound });
            tempCategories.Add(new CategoryFilter(category));
        }

        var categoriesCopy = new List<CategoryFilter>(Categories);
        CategoryColors.CategoriesColors.Clear();
        SoundItems.Clear();
        Categories.Clear();


        foreach (var categoryFilter in tempCategories.DistinctBy(c => c.Category.Split(Path.DirectorySeparatorChar)[0]))
        {
            var cached = categoriesCopy.Find(c => c.Category == categoryFilter.Category);

            if (cached != null)
            {
                categoryFilter.Enabled = cached.Enabled;
            }

            Categories.Add(categoryFilter);
        }

        foreach (var sound in tempSounds.OrderByDescending(s => s.topLevel).ThenBy(s => s.Category))
        {
            sound.Categorize();

            var category = Categories.First(q => q.Category == sound.Category);
            sound.IsVisible = category.Enabled;

            SoundItems.Add(sound);
        }
    }


    public void PlaySound(Sound? sound)
    {
        if (sound is null)
            throw new ArgumentNullException(nameof(sound));

        Debug.Assert(sound.FilePath != null, "sound.FilePath != null");
        // audioPlayer.Play(sound, fadeInTime);
        AudioPlaybackEngine.Instance.PlaySound(sound);
    }


    public void StopSound(Sound? sound)
    {
        if (sound is null)
            throw new ArgumentNullException(nameof(sound));

        Debug.Assert(sound.FilePath != null, "sound.FilePath != null");
        // audioPlayer.Play(sound, fadeInTime);
        AudioPlaybackEngine.Instance.StopSound(sound);
    }

    public void StopAllSounds()
    {
        AudioPlaybackEngine.Instance.StopAllSounds();
    }


    public partial class CategoryFilter(string category) : ObservableObject
    {
        [ObservableProperty] public string category = category;
        [ObservableProperty] public bool enabled = true;
    }
}