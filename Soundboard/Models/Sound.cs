using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Models
{
    public partial class Sound(string name) : ObservableObject
    {
        [ObservableProperty] public string? name = name;
        [ObservableProperty] public double progress = 0;
        [ObservableProperty] public bool isPlaying = false;
    }
}
