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
    public partial class Sound(string name, string? filePath = null, string? category = null, int group = 0)
        : ObservableObject
    {
        [ObservableProperty] public string? name = name;
        [ObservableProperty] public string? category = category;
        [ObservableProperty] public double progress = 0; //Random.Shared.NextDouble();
        [ObservableProperty] public bool isPlaying = false;
        [ObservableProperty] public bool isVisible = true;

        public int group { get; private set; } = group;
        public bool topLevel { get; set; } = false;

        public string? FilePath = filePath;

        private static readonly Dictionary<string, int> categoryGroups = new Dictionary<string, int>();
        private static readonly List<string> categoryIndexGroups = new List<string>();

        public static void ResetGroups()
        {
            categoryIndexGroups.Clear();
        }

        public void Categorize()
        {
            if (category is null) return;
            if (!category.Contains('\\')) return;


            if (categoryIndexGroups.Contains(category))
                group = categoryIndexGroups.FindIndex(q => q == category);
            else
            {
                categoryIndexGroups.Add(category);
                group = categoryIndexGroups.Count;
            }

            Category = Category?.Split('\\')[0];
        }
    }
}