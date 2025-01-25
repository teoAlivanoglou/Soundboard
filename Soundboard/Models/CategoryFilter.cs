using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Models;

public partial class CategoryFilter(string category, int childrenCount = 0) : ObservableObject
{
    [ObservableProperty] public string category = category;
    [ObservableProperty] public bool enabled = true;
    [ObservableProperty] public int childrenCount = childrenCount;
}