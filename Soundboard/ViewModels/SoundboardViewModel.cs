using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Models;
using System;
using System.CodeDom;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Soundboard.ViewModels;

public partial class SoundboardViewModel : ObservableObject
{
    public ObservableCollection<Sound> SoundItems { get; set; }
    [ObservableProperty] public int columns = 4;
    [ObservableProperty] public int minButtonSize = 70;
    [ObservableProperty] public int buttonGap = 3;
    //[ObservableProperty] public int extraGap = 0;


    public SoundboardViewModel()
    {
        SoundItems =
        [
            new Sound("Sound 1"),
            new Sound("Sound 2"),
            new Sound("Sound 3"),
            new Sound("Sound 4"),
            new Sound("Sound 5"),
            new Sound("Sound 6"),
            new Sound("Sound 7"),
            new Sound("Sound 8"),
            new Sound("Sound 9"),
            new Sound("Sound 10"),
        ];
    }

    internal static void WindowResized(object sender, SizeChangedEventArgs e)
    {
        var window = (MainWindow)sender;
        var newWidth = window.ActualWidth;
    }
}
