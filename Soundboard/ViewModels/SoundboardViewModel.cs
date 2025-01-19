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

// ReSharper disable InconsistentNaming

namespace Soundboard.ViewModels;

public partial class SoundboardViewModel : ObservableObject
{
    public ObservableCollection<Sound> SoundItems { get; set; }
    public ObservableCollection<CategoryFilter> Categories { get; set; }
    [ObservableProperty] public int columns = 4;
    [ObservableProperty] public int minButtonSize = 70;
    [ObservableProperty] public int buttonGap = 3;
    [ObservableProperty] public int idk = 0;
    [ObservableProperty] public bool settingsVisible = false;


    public AudioPlayer audioPlayer { get; private set; }
    [ObservableProperty] public int itemWidth = 70;

    private string lastChosenDirectory = string.Empty;

    public SoundboardViewModel()
    {
        if (!File.Exists(Path.Combine("Resources", "warning.mp3")))
        {
            var bytes = Resources.ResourceManager.GetObject("Warning");
            Directory.CreateDirectory("Resources");
            File.WriteAllBytesAsync(Path.Combine("Resources", "warning.mp3"), (byte[])bytes!);
        }

        SoundItems =
        [
            new Sound("Please Select a Folder", Path.Combine("Resources", "warning.mp3")),
        ];

        Categories = [new CategoryFilter("All")];

        audioPlayer = new AudioPlayer();
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

        foreach (var file in Directory.EnumerateFiles(filesystemUrl, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var category = Path.GetDirectoryName(Path.GetRelativePath(filesystemUrl, file));

            var topLevelSound = false;

            if (string.IsNullOrWhiteSpace(category))
            {
                category = Path.GetFileName(filesystemUrl);
                topLevelSound = true;
            }

            tempSounds.Add(new Sound(name, file, category) { topLevel = topLevelSound });
            tempCategories.Add(new CategoryFilter(category));
        }

        CategoryColors.CategoriesColors.Clear();
        SoundItems.Clear();
        Categories.Clear();

        foreach (var sound in tempSounds.OrderByDescending(s => s.topLevel).ThenBy(s => s.Category))
        {
            sound.Categorize();

            SoundItems.Add(sound);
        }

        foreach (var categoryFilter in tempCategories.DistinctBy(c => c.Category.Split('\\')[0]))
        {
            Categories.Add(categoryFilter);
        }
    }


    public void RefreshSounds()
    {
        Sound.ResetGroups();

        var tempCategories = new List<CategoryFilter>(10) { new("All") };
        if (tempCategories == null) throw new ArgumentNullException(nameof(tempCategories));

        var tempSounds = new List<Sound>(16) { };
        if (tempSounds == null) throw new ArgumentNullException(nameof(tempSounds));

        // TODO: Maybe have a 'levels' setting to determine how deep to go into the file tree

        foreach (var file in Directory.EnumerateFiles(lastChosenDirectory, "*", SearchOption.AllDirectories))
        {
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


        foreach (var categoryFilter in tempCategories.DistinctBy(c => c.Category.Split('\\')[0]))
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
        audioPlayer.Play(sound);
    }

    public void StopAllSounds()
    {
        audioPlayer.Abort();
    }


    public partial class CategoryFilter(string category) : ObservableObject
    {
        [ObservableProperty] public string category = category;
        [ObservableProperty] public bool enabled = true;
    }
}