using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

// ReSharper disable InconsistentNaming

namespace Soundboard.Models
{
    public partial class Sound(string name, string? filePath = null, string? category = null) : ObservableObject
    {
        [ObservableProperty] public string? name = name;
        [ObservableProperty] public string? category = category;
        [ObservableProperty] public double progress = 0; //Random.Shared.NextDouble();
        [ObservableProperty] public bool isPlaying = false;
        [ObservableProperty] public bool isVisible = true;


        // public int players = 0;
        public string? FilePath = filePath;
    }
}