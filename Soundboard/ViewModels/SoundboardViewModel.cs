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


    public SoundboardViewModel()
    {
        SoundItems =
        [
            new Sound("Sound 1"),
            new Sound("Sound 223442444"),
            new Sound("Sound 3"),
            new Sound("Sound 4"),
            new Sound("Sound 5"),
            new Sound("Sound 6"),
            new Sound("Sound 7"),
            new Sound("Sound 8"),
            new Sound("Sound 9"),
            new Sound("Sound 104564564565464 564564 564565466545t 56456546342 2s vxcvsfsdfsdfgg"),
        ];

        SoundItems[2].IsVisible = false;

        Categories = [];

        audioPlayer = new AudioPlayer();
    }

    public void ReadSounds(string filesystemUrl)
    {
        // Open the filesystem url and create a structure of sounds
        // Use the folder of each sound as the category
        SoundItems.Clear();

        foreach (var file in Directory.EnumerateFiles(filesystemUrl, "*", SearchOption.AllDirectories))
        {
            var name = Path.GetFileNameWithoutExtension(file);
            var category = Path.GetDirectoryName(Path.GetRelativePath(filesystemUrl, file));
            if (string.IsNullOrWhiteSpace(category))
                category = "Unsorted";
            var sound = new Sound(name, file, category);
            SoundItems.Add(sound);
            if (Categories.All(c => c.Category != category))
                Categories.Add(new CategoryFilter(category));

            // TODO: Actually find a solution for the unsorted sounds

        }
    }

    void CreatePlayerClass()
    {
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