using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using NAudio.Wave;

// ReSharper disable InconsistentNaming

namespace Soundboard.Models
{
    public partial class Sound : ObservableObject
    {
        [ObservableProperty] public string? name;
        [ObservableProperty] public string? category;
        [ObservableProperty] public double progress = 0; //Random.Shared.NextDouble();
        [ObservableProperty] public TimeSpan duration = TimeSpan.MinValue; //Random.Shared.NextDouble();
        [ObservableProperty] public bool isPlaying = false;
        [ObservableProperty] public bool isVisible = true;

        public int group { get; private set; }
        public bool topLevel { get; set; } = false;
        public string? FilePath;

        public float[] AudioData { get; private set; }
        public WaveFormat WaveFormat { get; private set; }


        private static readonly Dictionary<string, int> categoryGroups = new Dictionary<string, int>();
        private static readonly List<string> categoryIndexGroups = [];

        /// <inheritdoc/>
        public Sound(string name, string? filePath = null, string? category = null, bool cache = false, int group = 0)
        {
            this.name = name;
            this.category = category;
            this.group = group;
            FilePath = filePath;

            using var audioFileReader = new AudioFileReader(filePath);

            if (cache)
            {
                WaveFormat = audioFileReader.WaveFormat;
                var wholeFile = new List<float>((int)(audioFileReader.Length / 4));
                var readBuffer = new float[audioFileReader.WaveFormat.SampleRate * audioFileReader.WaveFormat.Channels];
                int samplesRead;
                while ((samplesRead = audioFileReader.Read(readBuffer, 0, readBuffer.Length)) > 0)
                {
                    wholeFile.AddRange(readBuffer.Take(samplesRead));
                }

                AudioData = wholeFile.ToArray();
            }

            duration = TimeSpan.FromSeconds(
                audioFileReader.WaveFormat.ExtraSize + (audioFileReader.Length / 4.0)
                / (double)(audioFileReader.WaveFormat.Channels * audioFileReader.WaveFormat.SampleRate));

        }

        public static void ResetGroups()
        {
            categoryIndexGroups.Clear();
        }

        public void Categorize()
        {

            if (category is null) return;
            if (!category.Contains(Path.DirectorySeparatorChar)) return;


            if (categoryIndexGroups.Contains(Category))
                group = categoryIndexGroups.FindIndex(q => q == Category) + 1;
            else
            {
                categoryIndexGroups.Add(Category);
                group = categoryIndexGroups.Count;
            }
            Category = Category?.Split(Path.DirectorySeparatorChar)[0];

        }
    }
}