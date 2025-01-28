using CommunityToolkit.Mvvm.ComponentModel;

namespace Soundboard.Models;

public partial class CategoryFilter(string? category, int childrenCount = 0) : ObservableObject
{
    [ObservableProperty] private string? category = category;
    [ObservableProperty] private bool enabled = true;
    [ObservableProperty] private int childrenCount = childrenCount;
}