using CommunityToolkit.Mvvm.ComponentModel;
using Soundboard.Avalonia.UI;

namespace Soundboard.Avalonia;

public partial class TestViewModel : ObservableObject
{
    [ObservableProperty] public partial DesignSoundModel Model { get; set; } = new();
}